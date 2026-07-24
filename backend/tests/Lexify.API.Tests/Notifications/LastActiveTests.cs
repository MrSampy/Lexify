using System.Net.Http.Json;
using System.Text.Json;
using Lexify.API.Tests.Infrastructure;
using Lexify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lexify.API.Tests.Notifications;

/// <summary>
/// <c>last_active_at</c> used to be written by nobody, so the admin user list showed a blank for every
/// account. These lock in that ordinary authenticated traffic stamps it.
/// </summary>
[Collection("Integration")]
public class LastActiveTests(LexifyWebApplicationFactory factory)
    : IClassFixture<LexifyWebApplicationFactory>
{
    [Fact]
    public async Task AuthenticatedRequest_StampsLastActive()
    {
        var email = $"active-{Guid.NewGuid():N}@example.com";
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync(email);

        (await client.GetAsync("/api/profile")).EnsureSuccessStatusCode();

        Assert.NotNull(await WaitForStampAsync(email));
    }

    [Fact]
    public async Task AnonymousRequest_LeavesLastActiveAlone()
    {
        var email = $"inactive-{Guid.NewGuid():N}@example.com";
        await RegisterWithoutUsingTheSessionAsync(email);

        // No bearer token: nothing to attribute the activity to.
        await factory.CreateClient().GetAsync("/api/auth/registration-status");

        Assert.Null(await GetStampAsync(email));
    }

    [Fact]
    public async Task AdminUserList_ShowsTheStamp()
    {
        var email = $"active-admin-view-{Guid.NewGuid():N}@example.com";
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync(email);
        (await client.GetAsync("/api/profile")).EnsureSuccessStatusCode();
        await WaitForStampAsync(email);

        var admin = await CreateAdminClientAsync();
        var users = await admin.GetFromJsonAsync<JsonElement>(
            $"/api/admin/users?email={email}&page=1&pageSize=20");

        var row = users.GetProperty("items").EnumerateArray()
            .Single(u => u.GetProperty("email").GetString() == email);
        Assert.NotEqual(JsonValueKind.Null, row.GetProperty("lastActiveAt").ValueKind);
    }

    [Fact]
    public async Task SecondRequestWithinTheWindow_DoesNotRewriteTheStamp()
    {
        var email = $"throttle-{Guid.NewGuid():N}@example.com";
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync(email);
        (await client.GetAsync("/api/profile")).EnsureSuccessStatusCode();
        var first = await WaitForStampAsync(email);

        for (var i = 0; i < 3; i++)
            (await client.GetAsync("/api/profile")).EnsureSuccessStatusCode();

        // Throttled to one write per user per 15 minutes — the value must be untouched.
        Assert.Equal(first, await GetStampAsync(email));
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    /// <summary>The stamp is written after the response is sent, so give it a moment to land.</summary>
    private async Task<DateTimeOffset?> WaitForStampAsync(string email)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var stamp = await GetStampAsync(email);
            if (stamp is not null) return stamp;
            await Task.Delay(100);
        }
        return null;
    }

    private async Task<DateTimeOffset?> GetStampAsync(string email)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Users
            .Where(u => u.Email == email)
            .Select(u => u.LastActiveAt)
            .FirstAsync();
    }

    private async Task RegisterWithoutUsingTheSessionAsync(string email)
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register",
            new { email, password = "Password1!", displayName = "Test User" });
        response.EnsureSuccessStatusCode();
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = factory.CreateClient();

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "admin@lexify.test", password = "Admin1234!" });
        login.EnsureSuccessStatusCode();

        var json = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", json.GetProperty("accessToken").GetString());

        return client;
    }
}
