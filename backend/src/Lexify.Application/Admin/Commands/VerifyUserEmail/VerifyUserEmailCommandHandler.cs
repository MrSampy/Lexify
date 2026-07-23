using System.Text.Json;
using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Admin.Commands.VerifyUserEmail;

public sealed class VerifyUserEmailCommandHandler(
    IUserRepository userRepository,
    IEmailVerificationTokenRepository verificationTokenRepository,
    IAuditService auditService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<VerifyUserEmailCommand, Result>
{
    public async Task<Result> Handle(VerifyUserEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.NotFound("User not found.");

        if (user.IsEmailVerified)
            return Result.Failure("This email address is already confirmed.");

        user.MarkEmailVerified();
        await userRepository.UpdateAsync(user, cancellationToken);

        // Any outstanding signup link is now pointless — burn it rather than leave a live token around.
        await verificationTokenRepository.InvalidateActiveForUserAsync(
            user.Id, EmailVerificationToken.Purposes.Signup, cancellationToken);

        await auditService.LogAsync(
            "verify_user_email", "User", user.Id.ToString(),
            oldValueJson: JsonSerializer.Serialize("unverified"),
            newValueJson: JsonSerializer.Serialize("verified"),
            ct: cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
