using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Lexify.Application.Abstractions;
using Lexify.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

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

    public DateTimeOffset GetExpiry() =>
        DateTimeOffset.UtcNow.AddMinutes(_settings.AccessTokenExpiryMinutes);
}
