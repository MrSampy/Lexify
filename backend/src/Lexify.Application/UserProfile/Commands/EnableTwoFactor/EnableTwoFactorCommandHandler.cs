using Lexify.Application.Auth.Common;
using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.UserProfile.Commands.EnableTwoFactor;

public sealed class EnableTwoFactorCommandHandler(
    IUserRepository userRepository,
    ITwoFactorService twoFactor)
    : IRequestHandler<EnableTwoFactorCommand, Result>
{
    public async Task<Result> Handle(EnableTwoFactorCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.NotFound("User not found.");

        if (user.TwoFactorEnabled)
            return Result.Failure("Two-factor authentication is already enabled.");

        // Codes are emailed, so enrolling with an unconfirmed address would risk a self-lockout.
        if (!user.IsEmailVerified)
            return Result.Failure("Confirm your email address before enabling two-factor authentication.");

        await twoFactor.IssueCodeAsync(user, cancellationToken);
        return Result.Ok();
    }
}
