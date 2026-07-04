using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Lexify.API.Tests.Infrastructure;
using Microsoft.IdentityModel.Tokens;

namespace Lexify.API.Tests.Auth;

[Collection("Integration")]
public class AuthControllerTests(LexifyWebApplicationFactory factory)
    : IClassFixture<LexifyWebApplicationFactory>
{
    [Fact]
    public async Task Register_Login_AccessProtectedEndpoint_Returns200()
    {
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync();

        // /api/stats is a simple authenticated endpoint
        var resp = await client.GetAsync("/api/stats");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task ExpiredAccessToken_Returns401()
    {
        // Create a JWT with expiry in the past using the known dev secret
        var expiredToken = CreateExpiredToken();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", expiredToken);

        var resp = await client.GetAsync("/api/stats");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_FromCookie_ReturnsNewAccessToken()
    {
        var (_, oldAccessToken, refreshToken) = await factory.CreateAuthenticatedClientAsync();
        var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"lexify_rt={refreshToken}");
        var resp = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var newToken = json.GetProperty("accessToken").GetString()!;
        Assert.NotEmpty(newToken);
        Assert.NotEqual(oldAccessToken, newToken);

        // Rotated refresh token must be delivered as a fresh HttpOnly cookie
        var setCookie = resp.Headers.GetValues("Set-Cookie")
            .First(c => c.StartsWith("lexify_rt=", StringComparison.Ordinal));
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RefreshToken_WithoutCookie_ReturnsBadRequest()
    {
        var client = factory.CreateClient();

        var resp = await client.PostAsync("/api/auth/refresh", null);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    private static string CreateExpiredToken()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(LexifyWebApplicationFactory.DevJwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: LexifyWebApplicationFactory.DevJwtIssuer,
            audience: LexifyWebApplicationFactory.DevJwtAudience,
            claims: [new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString())],
            notBefore: DateTime.UtcNow.AddMinutes(-10),
            expires: DateTime.UtcNow.AddMinutes(-5),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
