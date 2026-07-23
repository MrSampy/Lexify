using Lexify.Application.Abstractions;
using Lexify.Application.Auth.Commands.Common;
using Lexify.Application.Auth.Common;
using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Auth.Commands.VerifyTwoFactor;

public sealed class VerifyTwoFactorCommandHandler(
    IJwtService jwtService,
    IUserRepository userRepository,
    ITwoFactorService twoFactor,
    IAuthSessionService authSession)
    : IRequestHandler<VerifyTwoFactorCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(
        VerifyTwoFactorCommand request, CancellationToken cancellationToken)
    {
        var userId = await jwtService.ValidateTwoFactorChallengeToken(request.ChallengeToken);
        if (userId is null)
            return Result.Failure<AuthResponse>("Your sign-in session expired. Please sign in again.");

        var user = await userRepository.GetByIdAsync(userId.Value, cancellationToken);
        if (user is null || user.Status != User.Statuses.Active)
            return Result.Failure<AuthResponse>("Invalid or expired code.");

        // One generic message for wrong / expired / already-used / locked-out codes — never distinguish.
        if (!await twoFactor.VerifyCodeAsync(user, request.Code, cancellationToken))
            return Result.Failure<AuthResponse>("Invalid or expired code.");

        var session = await authSession.IssueAsync(
            user, request.IpAddress, request.UserAgent, cancellationToken);
        return Result.Ok(session);
    }
}
