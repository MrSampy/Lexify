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
/// Emails are swallowed by <see cref="NoOpEmailService"/>, so these tests read the issued token
/// straight out of the database — the same value the link in the email would have carried.
/// </summary>
[Collection("Integration")]
public class EmailVerificationTests(LexifyWebApplicationFactory factory)
    : IClassFixture<LexifyWebApplicationFactory>
{
    private const string Password = "Password1!";

    [Fact]
    public async Task Register_ThenLogin_IsRefusedUntilConfirmed_ThenSucceeds()
    {
        await SetVerificationRequiredAsync(true);
        var (client, email) = await RegisterAsync();

        var blocked = await LoginAsync(client, email);
        Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);
        var body = await blocked.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("email_not_verified", body.GetProperty("code").GetString());

        var verify = await client.PostAsJsonAsync("/api/auth/verify-email",
            new { token = await GetRawTokenAsync(email) });
        Assert.Equal(HttpStatusCode.OK, verify.StatusCode);

        var allowed = await LoginAsync(client, email);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Fact]
    public async Task VerifyEmail_SameTokenTwice_SecondIsRejected()
    {
        await SetVerificationRequiredAsync(true);
        var (client, email) = await RegisterAsync();
        var token = await GetRawTokenAsync(email);

        var first = await client.PostAsJsonAsync("/api/auth/verify-email", new { token });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await client.PostAsJsonAsync("/api/auth/verify-email", new { token });
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task VerifyEmail_ExpiredToken_IsRejected()
    {
        await SetVerificationRequiredAsync(true);
        var (client, email) = await RegisterAsync();
        var token = await GetRawTokenAsync(email);

        await ExpireTokensAsync(email);

        var response = await client.PostAsJsonAsync("/api/auth/verify-email", new { token });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await LoginAsync(client, email)).StatusCode);
    }

    [Fact]
    public async Task VerifyEmail_UnknownToken_IsRejected()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/verify-email",
            new { token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResendVerification_UnknownAddress_StillReturnsOk()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/resend-verification",
            new { email = $"nobody-{Guid.NewGuid():N}@example.com" });

        // Anti-enumeration: the response must look identical for a registered address.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ResendVerification_SupersedesThePreviousLink()
    {
        await SetVerificationRequiredAsync(true);
        var (client, email) = await RegisterAsync();
        var firstToken = await GetRawTokenAsync(email);

        var resend = await client.PostAsJsonAsync("/api/auth/resend-verification", new { email });
        Assert.Equal(HttpStatusCode.OK, resend.StatusCode);

        var secondToken = await GetRawTokenAsync(email);
        Assert.NotEqual(firstToken, secondToken);

        var stale = await client.PostAsJsonAsync("/api/auth/verify-email", new { token = firstToken });
        Assert.Equal(HttpStatusCode.BadRequest, stale.StatusCode);

        var fresh = await client.PostAsJsonAsync("/api/auth/verify-email", new { token = secondToken });
        Assert.Equal(HttpStatusCode.OK, fresh.StatusCode);
    }

    [Fact]
    public async Task VerificationDisabled_LoginWorksImmediately()
    {
        await SetVerificationRequiredAsync(false);
        var (client, email) = await RegisterAsync();

        var response = await LoginAsync(client, email);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SeededAdmin_PredatesVerification_CanStillSignIn()
    {
        // The migration grandfathers existing accounts; the seeded admin is one of them.
        await SetVerificationRequiredAsync(true);
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "admin@lexify.test", password = "Admin1234!" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── email change ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ChangeEmail_WrongPassword_IsRejected()
    {
        var (client, _) = await RegisterVerifiedAndSignInAsync();

        var response = await client.PutAsJsonAsync("/api/profile/email",
            new { newEmail = $"new-{Guid.NewGuid():N}@example.com", currentPassword = "WrongPass1!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangeEmail_AddressMovesOnlyAfterConfirmation()
    {
        var (client, oldEmail) = await RegisterVerifiedAndSignInAsync();
        var newEmail = $"moved-{Guid.NewGuid():N}@example.com";

        var request = await client.PutAsJsonAsync("/api/profile/email",
            new { newEmail, currentPassword = Password });
        Assert.Equal(HttpStatusCode.OK, request.StatusCode);

        // Still the old address, and the profile advertises the pending one.
        var profile = await client.GetFromJsonAsync<JsonElement>("/api/profile");
        Assert.Equal(oldEmail, profile.GetProperty("email").GetString());
        Assert.Equal(newEmail, profile.GetProperty("pendingEmail").GetString());

        var verify = await client.PostAsJsonAsync("/api/auth/verify-email",
            new { token = await GetRawTokenAsync(oldEmail) });
        Assert.Equal(HttpStatusCode.OK, verify.StatusCode);
        var result = await verify.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(result.GetProperty("emailChanged").GetBoolean());

        // The new address is now the login identity; the old one is gone.
        var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(anonymous, newEmail)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await LoginAsync(anonymous, oldEmail)).StatusCode);
    }

    [Fact]
    public async Task ChangeEmail_ToAnAddressAlreadyTaken_IsRejected()
    {
        var (_, takenEmail) = await RegisterVerifiedAndSignInAsync();
        var (client, _) = await RegisterVerifiedAndSignInAsync();

        var response = await client.PutAsJsonAsync("/api/profile/email",
            new { newEmail = takenEmail, currentPassword = Password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangeEmail_PendingLink_IsKilledByPasswordChange()
    {
        var (client, oldEmail) = await RegisterVerifiedAndSignInAsync();
        var newEmail = $"moved-{Guid.NewGuid():N}@example.com";

        (await client.PutAsJsonAsync("/api/profile/email",
            new { newEmail, currentPassword = Password })).EnsureSuccessStatusCode();

        // Grab the live confirmation token before it is invalidated.
        var token = await GetRawTokenAsync(oldEmail);

        // A password change must revoke any pending email-change link — otherwise an attacker's
        // outstanding link would survive the victim's response to a suspected compromise.
        (await client.PutAsJsonAsync("/api/profile/password",
            new { currentPassword = Password, newPassword = "Password2!" })).EnsureSuccessStatusCode();

        var confirm = await client.PostAsJsonAsync("/api/auth/verify-email", new { token });
        Assert.Equal(HttpStatusCode.BadRequest, confirm.StatusCode);

        // The address never moved and the pending change is cleared.
        var profile = await client.GetFromJsonAsync<JsonElement>("/api/profile");
        Assert.Equal(oldEmail, profile.GetProperty("email").GetString());
        Assert.Null(profile.GetProperty("pendingEmail").GetString());
    }

    [Fact]
    public async Task ChangeEmail_PendingLink_IsKilledByPasswordReset()
    {
        var (client, oldEmail) = await RegisterVerifiedAndSignInAsync();
        var newEmail = $"moved-{Guid.NewGuid():N}@example.com";

        (await client.PutAsJsonAsync("/api/profile/email",
            new { newEmail, currentPassword = Password })).EnsureSuccessStatusCode();

        var changeToken = await GetRawTokenAsync(oldEmail);

        // Drive the reset flow: forgot-password mints a reset token, reset-password consumes it.
        (await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = oldEmail }))
            .EnsureSuccessStatusCode();
        var resetToken = await GetRawResetTokenAsync(oldEmail);
        (await client.PostAsJsonAsync("/api/auth/reset-password",
            new { token = resetToken, newPassword = "Password3!" })).EnsureSuccessStatusCode();

        // The recovery path (reset) must also kill the pending email change.
        var confirm = await client.PostAsJsonAsync("/api/auth/verify-email", new { token = changeToken });
        Assert.Equal(HttpStatusCode.BadRequest, confirm.StatusCode);
    }

    [Fact]
    public async Task SuspendedUser_CannotCompleteEmailChange()
    {
        var (client, oldEmail) = await RegisterVerifiedAndSignInAsync();
        var newEmail = $"moved-{Guid.NewGuid():N}@example.com";

        (await client.PutAsJsonAsync("/api/profile/email",
            new { newEmail, currentPassword = Password })).EnsureSuccessStatusCode();
        var token = await GetRawTokenAsync(oldEmail);

        await SetUserStatusAsync(oldEmail, User.Statuses.Suspended);

        // A frozen account must not be able to rebind its login identity.
        var confirm = await client.PostAsJsonAsync("/api/auth/verify-email", new { token });
        Assert.Equal(HttpStatusCode.BadRequest, confirm.StatusCode);
    }

    // ── admin ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AdminVerifyEmail_ConfirmsUser_AndIsAudited()
    {
        await SetVerificationRequiredAsync(true);
        var (userClient, email) = await RegisterAsync();
        var userId = await GetUserIdAsync(email);

        var adminClient = await CreateAdminClientAsync();
        var verify = await adminClient.PutAsync($"/api/admin/users/{userId}/verify-email", null);
        Assert.Equal(HttpStatusCode.OK, verify.StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(userClient, email)).StatusCode);

        var audit = await adminClient.GetFromJsonAsync<JsonElement>(
            "/api/admin/audit?action=verify_user_email&page=1&pageSize=50");
        Assert.Contains(audit.GetProperty("items").EnumerateArray(),
            e => e.GetProperty("targetId").GetString() == userId.ToString());

        // The unconfirmed filter must no longer return them.
        var unconfirmed = await adminClient.GetFromJsonAsync<JsonElement>(
            "/api/admin/users?emailVerified=false&page=1&pageSize=200");
        Assert.DoesNotContain(unconfirmed.GetProperty("items").EnumerateArray(),
            e => e.GetProperty("email").GetString() == email);
    }

    [Fact]
    public async Task AdminVerifyEmail_RejectsNonAdmin()
    {
        var (client, _) = await RegisterVerifiedAndSignInAsync();

        var response = await client.PutAsync($"/api/admin/users/{Guid.NewGuid()}/verify-email", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task<(HttpClient Client, string Email)> RegisterAsync()
    {
        var client = factory.CreateClient();
        var email = $"verify-{Guid.NewGuid():N}@example.com";

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = Password, displayName = "Test User" });
        response.EnsureSuccessStatusCode();

        return (client, email);
    }

    /// <summary>Registers, confirms, and returns a client holding that user's bearer token.</summary>
    private async Task<(HttpClient Client, string Email)> RegisterVerifiedAndSignInAsync()
    {
        await SetVerificationRequiredAsync(true);
        var (client, email) = await RegisterAsync();

        (await client.PostAsJsonAsync("/api/auth/verify-email",
            new { token = await GetRawTokenAsync(email) })).EnsureSuccessStatusCode();

        var login = await LoginAsync(client, email);
        login.EnsureSuccessStatusCode();

        var json = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString());

        return (client, email);
    }

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email) =>
        client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = factory.CreateClient();

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "admin@lexify.test", password = "Admin1234!" });
        login.EnsureSuccessStatusCode();

        var json = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString());

        return client;
    }

    /// <summary>
    /// Only the hash is stored, so the raw token cannot be read back. Instead, mint a candidate and
    /// overwrite the pending token's hash with it — indistinguishable from the emailed value.
    /// </summary>
    private async Task<string> GetRawTokenAsync(string email)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var userId = await db.Users.Where(u => u.Email == email).Select(u => u.Id).FirstAsync();

        var token = await db.EmailVerificationTokens
            .Where(t => t.UserId == userId && t.UsedAt == null)
            .OrderByDescending(t => t.CreatedAt)
            .FirstAsync();

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        await db.EmailVerificationTokens
            .Where(t => t.Id == token.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.TokenHash, hash));

        return rawToken;
    }

    /// <summary>Same trick as <see cref="GetRawTokenAsync"/>, for the password-reset token table.</summary>
    private async Task<string> GetRawResetTokenAsync(string email)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var userId = await db.Users.Where(u => u.Email == email).Select(u => u.Id).FirstAsync();

        var token = await db.PasswordResetTokens
            .Where(t => t.UserId == userId && t.UsedAt == null)
            .OrderByDescending(t => t.CreatedAt)
            .FirstAsync();

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        await db.PasswordResetTokens
            .Where(t => t.Id == token.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.TokenHash, hash));

        return rawToken;
    }

    private Task SetUserStatusAsync(string email, string status) =>
        RunDbAsync(db => db.Users
            .Where(u => u.Email == email)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.Status, status)));

    private async Task RunDbAsync(Func<AppDbContext, Task> action)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await action(db);
    }

    private async Task ExpireTokensAsync(string email)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var userId = await db.Users.Where(u => u.Email == email).Select(u => u.Id).FirstAsync();

        await db.EmailVerificationTokens
            .Where(t => t.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(
                t => t.ExpiresAt, DateTimeOffset.UtcNow.AddMinutes(-1)));
    }

    private async Task<Guid> GetUserIdAsync(string email)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Users.Where(u => u.Email == email).Select(u => u.Id).FirstAsync();
    }

    private async Task SetVerificationRequiredAsync(bool required)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.SystemSettings
            .Where(s => s.Key == SystemSetting.Keys.EmailVerificationRequired)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Value, required ? "true" : "false"));
    }
}
