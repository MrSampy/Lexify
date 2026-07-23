using Lexify.Application.Auth.Common;
using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.UserProfile.Commands.ConfirmEnableTwoFactor;

public sealed class ConfirmEnableTwoFactorCommandHandler(
    IUserRepository userRepository,
    ITwoFactorService twoFactor)
    : IRequestHandler<ConfirmEnableTwoFactorCommand, Result>
{
    public async Task<Result> Handle(
        ConfirmEnableTwoFactorCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.NotFound("User not found.");

        if (!await twoFactor.VerifyCodeAsync(user, request.Code, cancellationToken))
            return Result.Failure("Invalid or expired code.");

        user.EnableTwoFactor();
        await userRepository.UpdateAsync(user, cancellationToken);
        // TransactionBehavior commits the flag on success.
        return Result.Ok();
    }
}
