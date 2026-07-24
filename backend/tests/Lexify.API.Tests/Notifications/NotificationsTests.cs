using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lexify.API.Tests.Infrastructure;
using Lexify.Application.Abstractions;
using Lexify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lexify.API.Tests.Notifications;

[Collection("Integration")]
public class NotificationsTests(LexifyWebApplicationFactory factory)
    : IClassFixture<LexifyWebApplicationFactory>
{
    [Fact]
    public async Task Profile_ReportsRemindersOn_ForANewAccount()
    {
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync();

        var profile = await client.GetFromJsonAsync<JsonElement>("/api/profile");

        Assert.True(profile.GetProperty("emailRemindersEnabled").GetBoolean());
    }

    [Fact]
    public async Task UpdateNotifications_TurnsRemindersOffAndBackOn()
    {
        var email = $"reminders-{Guid.NewGuid():N}@example.com";
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync(email);

        var off = await client.PutAsJsonAsync("/api/profile/notifications",
            new { emailRemindersEnabled = false });
        Assert.Equal(HttpStatusCode.OK, off.StatusCode);
        Assert.False(await GetFlagAsync(email));

        var profile = await client.GetFromJsonAsync<JsonElement>("/api/profile");
        Assert.False(profile.GetProperty("emailRemindersEnabled").GetBoolean());

        (await client.PutAsJsonAsync("/api/profile/notifications",
            new { emailRemindersEnabled = true })).EnsureSuccessStatusCode();
        Assert.True(await GetFlagAsync(email));
    }

    [Fact]
    public async Task Unsubscribe_WithTheLinkToken_WorksWithoutSigningIn()
    {
        var email = $"unsub-{Guid.NewGuid():N}@example.com";
        await factory.CreateAuthenticatedClientAsync(email);
        var token = MintToken(await GetUserIdAsync(email));

        // A brand-new client: no bearer token, exactly like opening the link from an inbox.
        var anonymous = factory.CreateClient();
        var response = await anonymous.PostAsJsonAsync("/api/notifications/unsubscribe", new { token });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(await GetFlagAsync(email));
    }

    [Fact]
    public async Task Unsubscribe_SameLinkTwice_StaysSuccessful()
    {
        var email = $"unsub-twice-{Guid.NewGuid():N}@example.com";
        await factory.CreateAuthenticatedClientAsync(email);
        var token = MintToken(await GetUserIdAsync(email));
        var anonymous = factory.CreateClient();

        (await anonymous.PostAsJsonAsync("/api/notifications/unsubscribe", new { token }))
            .EnsureSuccessStatusCode();
        var second = await anonymous.PostAsJsonAsync("/api/notifications/unsubscribe", new { token });

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
    }

    [Fact]
    public async Task Unsubscribe_TokenSignedForSomeoneElsesId_IsRejected()
    {
        var email = $"unsub-forge-{Guid.NewGuid():N}@example.com";
        await factory.CreateAuthenticatedClientAsync(email);
        var userId = await GetUserIdAsync(email);

        // The id half is swapped for the real victim's while keeping a signature minted for another
        // account — the shape a hand-rolled forgery would take.
        var otherToken = MintToken(Guid.NewGuid());
        var forged = $"{MintToken(userId).Split('.')[0]}.{otherToken.Split('.')[1]}";

        var response = await factory.CreateClient()
            .PostAsJsonAsync("/api/notifications/unsubscribe", new { token = forged });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(await GetFlagAsync(email));
    }

    [Theory]
    [InlineData("")]
    [InlineData("garbage")]
    [InlineData("not.a.token")]
    public async Task Unsubscribe_MalformedToken_IsRejected(string token)
    {
        var response = await factory.CreateClient()
            .PostAsJsonAsync("/api/notifications/unsubscribe", new { token });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private string MintToken(Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IUnsubscribeTokenService>().Create(userId);
    }

    private async Task<Guid> GetUserIdAsync(string email)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Users.Where(u => u.Email == email).Select(u => u.Id).FirstAsync();
    }

    private async Task<bool> GetFlagAsync(string email)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Users
            .Where(u => u.Email == email)
            .Select(u => u.EmailRemindersEnabled)
            .FirstAsync();
    }
}
