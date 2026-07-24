using System.Security.Cryptography;
using System.Text;
using Lexify.Application.Abstractions;
using Lexify.Application.Auth.Commands.Common;
using Lexify.Application.Auth.Common;
using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;
using DomainRefreshToken = Lexify.Domain.Entities.RefreshToken;

namespace Lexify.Application.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IJwtService jwtService)
    : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    /// <summary>
    /// How long a token stays usable after being rotated away. Two tabs — or a tab and the app's boot
    /// probe — can present the same cookie at once; the first rotates it and the second would otherwise
    /// be told "invalid token" and sign the user out of a perfectly healthy session. Inside this window
    /// the loser gets a fresh access token and the cookie is left alone, because the successor the
    /// winner already wrote is the valid one. Reuse of a token rotated away longer ago is still refused.
    /// </summary>
    private static readonly TimeSpan RotationGrace = TimeSpan.FromSeconds(30);

    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));
        var token = await refreshTokenRepository.GetByHashAsync(hash, cancellationToken);

        if (token is null)
            return Dead();

        if (!token.IsActive)
            return await ServeWithinGraceAsync(token, cancellationToken);

        var user = await userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user is null)
            return Dead();

        var newRawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));
        var newHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(newRawToken)));

        var newToken = token.Rotate(
            newHash,
            DateTimeOffset.UtcNow.Add(RefreshTokenLifetime),
            request.IpAddress,
            request.UserAgent);

        await refreshTokenRepository.UpdateAsync(token, cancellationToken);
        await refreshTokenRepository.AddAsync(newToken, cancellationToken);

        var accessToken = jwtService.GenerateAccessToken(user.Id, user.Email, user.Role);
        var expiresAt = jwtService.GetExpiry();

        return Result.Ok(new AuthResponse(accessToken, newRawToken, expiresAt));
    }

    /// <summary>
    /// The presented token is no longer active. Serve it anyway — without rotating — when it was rotated
    /// away moments ago and its successor is still live; otherwise it is expired, revoked by a logout or
    /// password change, or a genuine replay, and the session really is over.
    /// </summary>
    private async Task<Result<AuthResponse>> ServeWithinGraceAsync(
        DomainRefreshToken token, CancellationToken cancellationToken)
    {
        if (token.RevokedAt is null
            || token.ReplacedBy is null
            || DateTimeOffset.UtcNow - token.RevokedAt.Value > RotationGrace)
        {
            return Dead();
        }

        var successor = await refreshTokenRepository.GetByIdAsync(token.ReplacedBy.Value, cancellationToken);
        if (successor is null || !successor.IsActive)
            return Dead();

        var user = await userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user is null)
            return Dead();

        // A null refresh token means "leave the cookie as it is". Only the request that actually
        // rotated writes a Set-Cookie, so two responses racing back in either order can never leave the
        // browser holding the superseded value.
        return Result.Ok(new AuthResponse(
            jwtService.GenerateAccessToken(user.Id, user.Email, user.Role),
            null,
            jwtService.GetExpiry()));
    }

    /// <summary>
    /// Flags the one failure the API layer answers by clearing the refresh cookie: the token behind it
    /// can never work again. Everything transient must stay off this path — a single unlucky request
    /// evicting a live session is exactly the bug this distinction exists to prevent.
    /// </summary>
    private static Result<AuthResponse> Dead() =>
        Result.Failure<AuthResponse>(AuthErrorCodes.RefreshTokenDead);
}
