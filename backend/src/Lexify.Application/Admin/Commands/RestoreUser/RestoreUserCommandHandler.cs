using System.Text.Json;
using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Admin.Commands.RestoreUser;

public sealed class RestoreUserCommandHandler(
    IUserRepository userRepository,
    IAuditService auditService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RestoreUserCommand, Result>
{
    public async Task<Result> Handle(RestoreUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.NotFound("User not found.");

        var oldStatus = user.Status;
        user.Restore();
        await userRepository.UpdateAsync(user, cancellationToken);

        await auditService.LogAsync(
            "restore_user", "User", user.Id.ToString(),
            oldValueJson: JsonSerializer.Serialize(oldStatus),
            newValueJson: JsonSerializer.Serialize(user.Status),
            ct: cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
