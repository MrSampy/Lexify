using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Lexify.Application.Abstractions;
using Lexify.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace Lexify.Infrastructure.Services;

public sealed class JwtService(IOptions<JwtSettings> options) : IJwtService
{
    private readonly JwtSettings _settings = options.Value;

    public string GenerateAccessToken(Guid userId, string email, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        Claim[] claims =
        [
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        ];

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateImpersonationToken(Guid targetUserId, string targetEmail, string targetRole, Guid adminId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        Claim[] claims =
        [
            new Claim(JwtRegisteredClaimNames.Sub, targetUserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, targetEmail),
            new Claim(ClaimTypes.Role, targetRole),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("impersonated_by", adminId.ToString())
        ];

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public DateTimeOffset GetExpiry() =>
        DateTimeOffset.UtcNow.AddMinutes(_settings.AccessTokenExpiryMinutes);

    /// <summary>Marks a token as the 2FA challenge rather than an access token — see the audience note below.</summary>
    private const string TwoFactorPurpose = "twofa_challenge";

    // A distinct audience so the JwtBearer pipeline (ValidAudience = _settings.Audience) rejects a
    // challenge token against every [Authorize] endpoint: it must never double as an access token.
    private string TwoFactorAudience => _settings.Audience + ":2fa";

    // Use the modern JsonWebTokenHandler (Microsoft.IdentityModel.JsonWebTokens) here — it is the one
    // .NET 8's JwtBearer uses to validate access tokens, and it is version-aligned with the 8.x
    // Microsoft.IdentityModel.Tokens on the load path. The legacy JwtSecurityTokenHandler (7.x) mis-reads
    // claims against those 8.x types.
    public string GenerateTwoFactorChallengeToken(Guid userId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _settings.Issuer,
            Audience = TwoFactorAudience,
            Expires = DateTime.UtcNow.AddMinutes(_settings.TwoFactorChallengeExpiryMinutes),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = userId.ToString(),
                ["purpose"] = TwoFactorPurpose,
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString()
            }
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    public async Task<Guid?> ValidateTwoFactorChallengeToken(string token)
    {
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _settings.Issuer,
            ValidAudience = TwoFactorAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey)),
            ClockSkew = TimeSpan.Zero
        };

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, parameters);
        if (!result.IsValid)
            return null;

        if (!result.Claims.TryGetValue("purpose", out var purpose)
            || purpose as string != TwoFactorPurpose)
            return null;

        return result.Claims.TryGetValue(JwtRegisteredClaimNames.Sub, out var sub)
            && Guid.TryParse(sub as string, out var userId)
            ? userId
            : null;
    }
}
