using System.Security.Cryptography;
using System.Text;
using Lexify.Application.Abstractions;
using Lexify.Application.Auth.Commands.Common;
using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;
using DomainRefreshToken = Lexify.Domain.Entities.RefreshToken;

namespace Lexify.Application.Auth.Commands.Login;

public sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher passwordHasher,
    IJwtService jwtService)
    : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        // Same message for both "not found" and "wrong password" to prevent user enumeration.
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result.Failure<AuthResponse>("Invalid credentials.");

        if (user.Status != User.Statuses.Active)
            return Result.Failure<AuthResponse>("Account is not active.");

        var accessToken = jwtService.GenerateAccessToken(user.Id, user.Email, user.Role);
        var expiresAt = jwtService.GetExpiry();

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        var refreshToken = new DomainRefreshToken(
            user.Id,
            tokenHash,
            DateTimeOffset.UtcNow.Add(RefreshTokenLifetime),
            request.IpAddress,
            request.UserAgent);

        await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        return Result.Ok(new AuthResponse(accessToken, rawToken, expiresAt));
    }
}
