using Lexify.Application.Abstractions;
using Lexify.Application.Auth.Common;
using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.UserProfile.Commands.RequestEmailChange;

public sealed class RequestEmailChangeCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IEmailVerificationService emailVerification)
    : IRequestHandler<RequestEmailChangeCommand, Result>
{
    public async Task<Result> Handle(
        RequestEmailChangeCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.NotFound("User not found.");

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            return Result.Failure("Current password is incorrect.");

        var newEmail = request.NewEmail.ToLowerInvariant().Trim();

        if (newEmail == user.Email)
            return Result.Failure("That is already your email address.");

        var existing = await userRepository.GetByEmailAsync(newEmail, cancellationToken);
        if (existing is not null)
            return Result.Failure("That email address is already in use.");

        // The address on the account is NOT touched here. It moves only once the link sent to the new
        // inbox is opened — otherwise a typo would lock the owner out of their own account for good.
        await emailVerification.IssueAsync(
            user, EmailVerificationToken.Purposes.EmailChange, newEmail, cancellationToken);

        return Result.Ok();
    }
}
