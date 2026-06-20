using System.Security.Cryptography;
using System.Text;
using Lexify.Application.Abstractions;
using Lexify.Application.Auth.Commands.Common;
using Lexify.Application.Common;
using Lexify.Domain.Entities;
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

    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));
        var token = await refreshTokenRepository.GetByHashAsync(hash, cancellationToken);

        if (token is null || !token.IsActive)
            return Result.Failure<AuthResponse>("Invalid or expired refresh token.");

        var user = await userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<AuthResponse>("User not found.");

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
}
