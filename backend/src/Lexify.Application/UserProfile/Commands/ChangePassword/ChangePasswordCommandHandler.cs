using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.UserProfile.Commands.ChangePassword;

public sealed class ChangePasswordCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IEmailVerificationTokenRepository emailVerificationTokenRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ChangePasswordCommand, Result>
{
    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.NotFound("User not found.");

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            return Result.Failure("Current password is incorrect.");

        user.ChangePassword(passwordHasher.Hash(request.NewPassword));
        await userRepository.UpdateAsync(user, cancellationToken);

        // Standard practice: a password change revokes every other session.
        await refreshTokenRepository.RevokeAllForUserAsync(user.Id, cancellationToken);

        // A pending email change is a standing takeover foothold: if the password is being changed
        // because of a suspected compromise, an attacker's outstanding link must not survive it.
        await emailVerificationTokenRepository.InvalidateActiveForUserAsync(
            user.Id, EmailVerificationToken.Purposes.EmailChange, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
