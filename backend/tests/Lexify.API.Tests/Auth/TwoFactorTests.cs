using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lexify.API.Tests.Infrastructure;
using Lexify.Domain.Entities;
using Lexify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lexify.API.Tests.Auth;

/// <summary>
/// Emails are swallowed by <see cref="NoOpEmailService"/>, so these tests overwrite the issued code's
/// hash in the database with the hash of a known value ("123456") — indistinguishable from the emailed
/// code. Rate limiting is disabled in the test host, so the 429 verify budget is not exercised here.
/// </summary>
[Collection("Integration")]
public class TwoFactorTests(LexifyWebApplicationFactory factory)
    : IClassFixture<LexifyWebApplicationFactory>
{
    private const string Password = "Password1!";
    private const string KnownCode = "123456";
    private const string AdminEmail = "admin@lexify.test";
    private const string AdminPassword = "Admin1234!";

    // ── mandatory admin path ─────────────────────────────────────────────────

    [Fact]
    public async Task Admin_GlobalOn_LoginReturnsChallengeWithoutSession_ThenVerifySucceeds()
    {
        await SetTwoFactorEnabledAsync(true);

        var client = factory.CreateClient();
        var step1 = await client.PostAsJsonAsync("/api/auth/login",
            new { email = AdminEmail, password = AdminPassword });

        Assert.Equal(HttpStatusCode.OK, step1.StatusCode);
        var body = await step1.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("twoFactorRequired").GetBoolean());
        var challenge = body.GetProperty("challengeToken").GetString();
        Assert.False(string.IsNullOrEmpty(challenge));
        // The challenge path must not hand out a session.
        Assert.False(body.TryGetProperty("accessToken", out _));
        Assert.DoesNotContain(SetCookies(step1), c => c.StartsWith("lexify_rt=", StringComparison.Ordinal));

        await ForgeCodeAsync(AdminEmail, KnownCode);

        var step2 = await client.PostAsJsonAsync("/api/auth/login/verify-2fa",
            new { challengeToken = challenge, code = KnownCode });

        Assert.Equal(HttpStatusCode.OK, step2.StatusCode);
        var session = await step2.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrEmpty(session.GetProperty("accessToken").GetString()));
        // Now the refresh cookie is set.
        Assert.Contains(SetCookies(step2), c => c.StartsWith("lexify_rt=", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Admin_GlobalOff_LogsInDirectlyWithoutChallenge()
    {
        await SetTwoFactorEnabledAsync(false);

        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email = AdminEmail, password = AdminPassword });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(body.TryGetProperty("twoFactorRequired", out _));
        Assert.False(string.IsNullOrEmpty(body.GetProperty("accessToken").GetString()));
    }

    [Fact]
    public async Task VerifyTwoFactor_WrongCodeFiveTimes_LocksOutEvenTheCorrectCode()
    {
        await SetTwoFactorEnabledAsync(true);

        var client = factory.CreateClient();
        var challenge = await StartAdminChallengeAsync(client);
        await ForgeCodeAsync(AdminEmail, KnownCode);

        for (var i = 0; i < LoginTwoFactorCode.MaxAttempts; i++)
        {
            var wrong = await client.PostAsJsonAsync("/api/auth/login/verify-2fa",
                new { challengeToken = challenge, code = "000000" });
            Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        }

        // The code is now spent by attempts, so even the correct value is refused.
        var correct = await client.PostAsJsonAsync("/api/auth/login/verify-2fa",
            new { challengeToken = challenge, code = KnownCode });
        Assert.Equal(HttpStatusCode.BadRequest, correct.StatusCode);
    }

    [Fact]
    public async Task VerifyTwoFactor_TamperedChallenge_IsRejected()
    {
        await SetTwoFactorEnabledAsync(true);
        var client = factory.CreateClient();
        var challenge = await StartAdminChallengeAsync(client);
        await ForgeCodeAsync(AdminEmail, KnownCode);

        var tampered = challenge![..^2] + (challenge[^1] == 'a' ? "bb" : "aa");
        var response = await client.PostAsJsonAsync("/api/auth/login/verify-2fa",
            new { challengeToken = tampered, code = KnownCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task VerifyTwoFactor_DeadChallenge_CarriesMachineReadableCode()
    {
        await SetTwoFactorEnabledAsync(true);
        var client = factory.CreateClient();
        var challenge = await StartAdminChallengeAsync(client);
        await ForgeCodeAsync(AdminEmail, KnownCode);

        // A tampered/dead challenge must be told apart from a wrong code so the client restarts sign-in.
        var tampered = challenge![..^2] + (challenge[^1] == 'a' ? "bb" : "aa");
        var response = await client.PostAsJsonAsync("/api/auth/login/verify-2fa",
            new { challengeToken = tampered, code = KnownCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("two_factor_challenge_expired", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task VerifyTwoFactor_WrongCode_StaysOnCodeStep_WithoutChallengeExpiredCode()
    {
        await SetTwoFactorEnabledAsync(true);
        var client = factory.CreateClient();
        var challenge = await StartAdminChallengeAsync(client);
        await ForgeCodeAsync(AdminEmail, KnownCode);

        // A valid challenge but the wrong code: a generic 400 with NO machine-readable code, so the
        // client keeps the user on the code step instead of bouncing them back to the password form.
        var response = await client.PostAsJsonAsync("/api/auth/login/verify-2fa",
            new { challengeToken = challenge, code = "000000" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(body.TryGetProperty("code", out _));
    }

    [Fact]
    public async Task VerifyTwoFactor_UsedCode_CannotBeReplayed()
    {
        await SetTwoFactorEnabledAsync(true);
        var client = factory.CreateClient();
        var challenge = await StartAdminChallengeAsync(client);
        await ForgeCodeAsync(AdminEmail, KnownCode);

        // First verify consumes the code atomically.
        var first = await client.PostAsJsonAsync("/api/auth/login/verify-2fa",
            new { challengeToken = challenge, code = KnownCode });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        // The challenge token is still valid, but the code is spent — a replay must not yield a session.
        var replay = await client.PostAsJsonAsync("/api/auth/login/verify-2fa",
            new { challengeToken = challenge, code = KnownCode });
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
    }

    [Fact]
    public async Task ChallengeToken_CannotBeUsedAsAnAccessToken()
    {
        await SetTwoFactorEnabledAsync(true);
        var client = factory.CreateClient();
        var challenge = await StartAdminChallengeAsync(client);

        // A distinct audience means the bearer pipeline must reject it on every [Authorize] endpoint.
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", challenge);
        var response = await client.GetAsync("/api/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── opt-in path for regular users ────────────────────────────────────────

    [Fact]
    public async Task User_EnableConfirm_ThenLoginRequiresCode_ThenDisable_LoginIsDirectAgain()
    {
        await SetTwoFactorEnabledAsync(false);
        var (client, email) = await RegisterVerifiedAndSignInAsync();

        // Enable → a code is issued; confirm with it.
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/profile/2fa/enable", null)).StatusCode);
        await ForgeCodeAsync(email, KnownCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsJsonAsync("/api/profile/2fa/confirm", new { code = KnownCode })).StatusCode);

        // With the feature on and the user opted in, a fresh sign-in now demands a code.
        await SetTwoFactorEnabledAsync(true);
        var anon = factory.CreateClient();
        var step1 = await anon.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        var body = await step1.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("twoFactorRequired").GetBoolean());

        // Disable (re-authenticated by password) → sign-in stops asking for a code.
        Assert.Equal(HttpStatusCode.OK,
            (await client.SendAsync(DeleteWithBody("/api/profile/2fa", new { currentPassword = Password }))).StatusCode);

        var direct = await anon.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        var directBody = await direct.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(directBody.TryGetProperty("twoFactorRequired", out _));
        Assert.False(string.IsNullOrEmpty(directBody.GetProperty("accessToken").GetString()));
    }

    [Fact]
    public async Task DisableTwoFactor_WrongPassword_IsRejected()
    {
        await SetTwoFactorEnabledAsync(false);
        var (client, _) = await RegisterVerifiedAndSignInAsync();

        var response = await client.SendAsync(
            DeleteWithBody("/api/profile/2fa", new { currentPassword = "WrongPass1!" }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DisableTwoFactor_ByAdmin_IsForbidden()
    {
        await SetTwoFactorEnabledAsync(true);
        var adminClient = await SignInAdminThroughTwoFactorAsync();

        var response = await adminClient.SendAsync(
            DeleteWithBody("/api/profile/2fa", new { currentPassword = AdminPassword }));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static async Task<string?> StartAdminChallengeAsync(HttpClient client)
    {
        var step1 = await client.PostAsJsonAsync("/api/auth/login",
            new { email = AdminEmail, password = AdminPassword });
        step1.EnsureSuccessStatusCode();
        var body = await step1.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("challengeToken").GetString();
    }

    private async Task<HttpClient> SignInAdminThroughTwoFactorAsync()
    {
        var client = factory.CreateClient();
        var challenge = await StartAdminChallengeAsync(client);
        await ForgeCodeAsync(AdminEmail, KnownCode);

        var step2 = await client.PostAsJsonAsync("/api/auth/login/verify-2fa",
            new { challengeToken = challenge, code = KnownCode });
        step2.EnsureSuccessStatusCode();

        var json = await step2.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString());
        return client;
    }

    private async Task<(HttpClient Client, string Email)> RegisterVerifiedAndSignInAsync()
    {
        var client = factory.CreateClient();
        var email = $"2fa-{Guid.NewGuid():N}@example.com";

        (await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = Password, displayName = "Test User" })).EnsureSuccessStatusCode();

        // Mark verified directly so the confirmation gate doesn't get in the way.
        await MarkEmailVerifiedAsync(email);

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        login.EnsureSuccessStatusCode();
        var json = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString());

        return (client, email);
    }

    /// <summary>Overwrites the user's most recent unused code with the hash of a known value.</summary>
    private async Task ForgeCodeAsync(string email, string code)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var userId = await db.Users.Where(u => u.Email == email).Select(u => u.Id).FirstAsync();
        var entity = await db.LoginTwoFactorCodes
            .Where(c => c.UserId == userId && c.UsedAt == null)
            .OrderByDescending(c => c.CreatedAt)
            .FirstAsync();

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
        await db.LoginTwoFactorCodes
            .Where(c => c.Id == entity.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.CodeHash, hash));
    }

    private async Task MarkEmailVerifiedAsync(string email) =>
        await RunDbAsync(db => db.Users
            .Where(u => u.Email == email)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.EmailVerifiedAt, DateTimeOffset.UtcNow)));

    private Task SetTwoFactorEnabledAsync(bool enabled) =>
        RunDbAsync(db => db.SystemSettings
            .Where(s => s.Key == SystemSetting.Keys.TwoFactorEnabled)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Value, enabled ? "true" : "false")));

    private async Task RunDbAsync(Func<AppDbContext, Task> action)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await action(db);
    }

    private static IEnumerable<string> SetCookies(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values) ? values : [];

    private static HttpRequestMessage DeleteWithBody(string url, object body) =>
        new(HttpMethod.Delete, url) { Content = JsonContent.Create(body) };
}
