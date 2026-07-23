using Lexify.Application.Abstractions;
using Lexify.Application.Auth.Common;
using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Auth.Commands.VerifyEmail;

public sealed class VerifyEmailCommandHandler(
    IEmailVerificationTokenRepository tokenRepository,
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IBackgroundJobService backgroundJobService)
    : IRequestHandler<VerifyEmailCommand, Result<VerifyEmailResultDto>>
{
    // One message for every token failure, so the endpoint leaks nothing about token state.
    private const string InvalidTokenMessage = "Invalid or expired link.";

    public async Task<Result<VerifyEmailResultDto>> Handle(
        VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = IEmailVerificationService.HashToken(request.Token);
        var token = await tokenRepository.GetByHashAsync(tokenHash, cancellationToken);

        if (token is null || !token.IsActive)
            return Result.Failure<VerifyEmailResultDto>(InvalidTokenMessage);

        // Only an active account may complete verification: a suspended one must not be able to confirm
        // itself or rebind its login identity via a pending email-change link. Matches ResendVerification.
        var user = await userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user is null || user.Status != User.Statuses.Active)
            return Result.Failure<VerifyEmailResultDto>(InvalidTokenMessage);

        var isEmailChange = token.Purpose == EmailVerificationToken.Purposes.EmailChange;

        // Captured before ChangeEmail mutates it, so the change notice can reach the former address.
        var previousEmail = user.Email;

        if (isEmailChange)
        {
            // The link is valid for a day; someone else may have registered that address meanwhile.
            var occupant = await userRepository.GetByEmailAsync(token.NewEmail!, cancellationToken);
            if (occupant is not null && occupant.Id != user.Id)
                return Result.Failure<VerifyEmailResultDto>(
                    "That email address is already in use by another account.");

            user.ChangeEmail(token.NewEmail!);
        }

        user.MarkEmailVerified();
        await userRepository.UpdateAsync(user, cancellationToken);

        token.MarkUsed();

        if (isEmailChange)
        {
            // The address is the login identity: changing it must invalidate sessions established
            // under the old one, the same way a password change does.
            await refreshTokenRepository.RevokeAllForUserAsync(user.Id, cancellationToken);

            // Warn the old inbox: if the account was moved by someone else, this is the owner's last
            // notice before password recovery starts flowing to the new (attacker's) address.
            backgroundJobService.EnqueueEmailChangedNotice(previousEmail, user.Email);
        }
        else
        {
            // Held back at registration so the confirmation email arrived alone.
            var username = user.DisplayName is { Length: > 0 } name ? name : user.Email.Split('@')[0];
            backgroundJobService.EnqueueWelcomeEmail(user.Email, username);
        }

        return Result.Ok(new VerifyEmailResultDto(user.Email, isEmailChange));
    }
}
