using System.Text.Json;
using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Admin.Commands.ImpersonateUser;

public sealed class ImpersonateUserCommandHandler(
    IUserRepository userRepository,
    IAuditService auditService,
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

        await auditService.LogAsync(
            "impersonate_user", "User", target.Id.ToString(),
            newValueJson: JsonSerializer.Serialize($"impersonated as {target.Email}"),
            ct: cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok(token);
    }
}
