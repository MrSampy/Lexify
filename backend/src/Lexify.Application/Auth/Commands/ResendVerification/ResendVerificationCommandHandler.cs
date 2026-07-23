using Lexify.Application.Auth.Common;
using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Auth.Commands.ResendVerification;

public sealed class ResendVerificationCommandHandler(
    IUserRepository userRepository,
    IEmailVerificationService emailVerification)
    : IRequestHandler<ResendVerificationCommand, Result>
{
    public async Task<Result> Handle(
        ResendVerificationCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        // Always succeed: like forgot-password, the response must not reveal whether the address is
        // registered — or, here, whether it is still waiting on confirmation.
        if (user is null || user.Status != User.Statuses.Active || user.IsEmailVerified)
            return Result.Ok();

        await emailVerification.IssueAsync(
            user, EmailVerificationToken.Purposes.Signup, ct: cancellationToken);

        return Result.Ok();
    }
}
