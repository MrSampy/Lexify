using Lexify.Application.Abstractions;
using Lexify.Application.Auth.Common;
using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Auth.Commands.ResendTwoFactorCode;

public sealed class ResendTwoFactorCodeCommandHandler(
    IJwtService jwtService,
    IUserRepository userRepository,
    ITwoFactorService twoFactor)
    : IRequestHandler<ResendTwoFactorCodeCommand, Result>
{
    public async Task<Result> Handle(ResendTwoFactorCodeCommand request, CancellationToken cancellationToken)
    {
        var userId = await jwtService.ValidateTwoFactorChallengeToken(request.ChallengeToken);
        if (userId is null)
            return Result.Failure("Your sign-in session expired. Please sign in again.");

        var user = await userRepository.GetByIdAsync(userId.Value, cancellationToken);

        // Only re-issue when 2FA genuinely still applies; otherwise silently succeed (nothing to resend).
        if (user is not null
            && user.Status == User.Statuses.Active
            && await twoFactor.IsRequiredForAsync(user, cancellationToken))
        {
            await twoFactor.IssueCodeAsync(user, cancellationToken);
        }

        return Result.Ok();
    }
}
