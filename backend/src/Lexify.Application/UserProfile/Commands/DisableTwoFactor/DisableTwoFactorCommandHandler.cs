using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.UserProfile.Commands.DisableTwoFactor;

public sealed class DisableTwoFactorCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher)
    : IRequestHandler<DisableTwoFactorCommand, Result>
{
    public async Task<Result> Handle(DisableTwoFactorCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.NotFound("User not found.");

        // Re-authenticate: disabling a security control must not ride on a stolen session alone.
        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            return Result.Failure("Current password is incorrect.");

        if (user.IsTwoFactorMandatory)
            return Result.Forbidden(
                "Two-factor authentication is required for administrators and cannot be disabled.");

        user.DisableTwoFactor();
        await userRepository.UpdateAsync(user, cancellationToken);
        return Result.Ok();
    }
}
