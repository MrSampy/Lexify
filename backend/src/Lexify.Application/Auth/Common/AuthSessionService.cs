using System.Security.Cryptography;
using System.Text;
using Lexify.Application.Abstractions;
using Lexify.Application.Auth.Commands.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using DomainRefreshToken = Lexify.Domain.Entities.RefreshToken;

namespace Lexify.Application.Auth.Common;

/// <summary>
/// Mints an authenticated session (access token + stored refresh token) for a user. Extracted from the
/// login handler so the no-2FA path and the post-2FA verify path issue sessions identically instead of
/// duplicating the token-minting tail (and drifting over time).
/// </summary>
public interface IAuthSessionService
{
    Task<AuthResponse> IssueAsync(
        User user, string? ipAddress, string? userAgent, CancellationToken ct = default);
}

public sealed class AuthSessionService(
    IRefreshTokenRepository refreshTokenRepository,
    IJwtService jwtService) : IAuthSessionService
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    public async Task<AuthResponse> IssueAsync(
        User user, string? ipAddress, string? userAgent, CancellationToken ct = default)
    {
        var accessToken = jwtService.GenerateAccessToken(user.Id, user.Email, user.Role);
        var expiresAt = jwtService.GetExpiry();

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        var refreshToken = new DomainRefreshToken(
            user.Id,
            tokenHash,
            DateTimeOffset.UtcNow.Add(RefreshTokenLifetime),
            ipAddress,
            userAgent);

        await refreshTokenRepository.AddAsync(refreshToken, ct);

        return new AuthResponse(accessToken, rawToken, expiresAt);
    }
}
