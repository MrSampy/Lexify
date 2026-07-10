using System.Text.Json;
using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Admin.Commands.ImpersonateUser;

public sealed class ImpersonateUserCommandHandler(
    IUserRepository userRepository,
    IAuditLogRepository auditLogRepository,
    IJwtService jwtService,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ImpersonateUserCommand, Result<string>>
{
    public async Task<Result<string>> Handle(ImpersonateUserCommand request, CancellationToken cancellationToken)
    {
        var target = await userRepository.GetByIdAsync(request.TargetUserId, cancellationToken);
        if (target is null)
            return Result.NotFound<string>("User not found.");

        var token = jwtService.GenerateImpersonationToken(
            target.Id, target.Email, target.Role, currentUser.UserId);

        var log = new AuditLog(
            adminId: currentUser.UserId,
            action: "impersonate_user",
            targetType: "User",
            targetId: target.Id.ToString(),
            newValue: JsonSerializer.Serialize($"impersonated as {target.Email}"));

        await auditLogRepository.AddAsync(log, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok(token);
    }
}
