using System.Text.Json;
using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Admin.Commands.SuspendUser;

public sealed class SuspendUserCommandHandler(
    IUserRepository userRepository,
    IAuditService auditService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SuspendUserCommand, Result>
{
    public async Task<Result> Handle(SuspendUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.NotFound("User not found.");

        var oldStatus = user.Status;
        user.Suspend();
        await userRepository.UpdateAsync(user, cancellationToken);

        await auditService.LogAsync(
            "suspend_user", "User", user.Id.ToString(),
            oldValueJson: JsonSerializer.Serialize(oldStatus),
            newValueJson: JsonSerializer.Serialize(user.Status),
            ct: cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
