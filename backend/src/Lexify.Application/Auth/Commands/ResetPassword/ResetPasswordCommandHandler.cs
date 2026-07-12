using System.Security.Cryptography;
using System.Text;
using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Auth.Commands.ResetPassword;

public sealed class ResetPasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordResetTokenRepository passwordResetTokenRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher passwordHasher)
    : IRequestHandler<ResetPasswordCommand, Result>
{
    // One generic message for every token failure so the endpoint leaks nothing about token state.
    private const string InvalidTokenMessage = "Invalid or expired token.";

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));
        var token = await passwordResetTokenRepository.GetByHashAsync(tokenHash, cancellationToken);

        if (token is null || !token.IsActive)
            return Result.Failure(InvalidTokenMessage);

        var user = await userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user is null || user.Status != User.Statuses.Active)
            return Result.Failure(InvalidTokenMessage);

        user.ChangePassword(passwordHasher.Hash(request.NewPassword));
        await userRepository.UpdateAsync(user, cancellationToken);

        token.MarkUsed();

        // Standard practice: a password reset revokes every existing session.
        await refreshTokenRepository.RevokeAllForUserAsync(user.Id, cancellationToken);

        return Result.Ok();
    }
}
