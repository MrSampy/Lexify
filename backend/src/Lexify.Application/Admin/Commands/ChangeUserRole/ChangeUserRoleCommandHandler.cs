using System.Text.Json;
using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Admin.Commands.ChangeUserRole;

public sealed class ChangeUserRoleCommandHandler(
    IUserRepository userRepository,
    IAuditService auditService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ChangeUserRoleCommand, Result>
{
    public async Task<Result> Handle(ChangeUserRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.NotFound("User not found.");

        var oldRole = user.Role;
        user.ChangeRole(request.Role);
        await userRepository.UpdateAsync(user, cancellationToken);

        await auditService.LogAsync(
            "change_user_role", "User", user.Id.ToString(),
            oldValueJson: JsonSerializer.Serialize(oldRole),
            newValueJson: JsonSerializer.Serialize(user.Role),
            ct: cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
