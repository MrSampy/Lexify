using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Auth.Queries.GetRegistrationStatus;

/// <summary>
/// Anonymous endpoint: tells the sign-up form whether to ask for an invite code. It deliberately
/// exposes only the two booleans — never the invite code itself.
/// </summary>
public sealed class GetRegistrationStatusQueryHandler(ISystemSettingRepository settingRepository)
    : IRequestHandler<GetRegistrationStatusQuery, Result<RegistrationStatusDto>>
{
    public async Task<Result<RegistrationStatusDto>> Handle(
        GetRegistrationStatusQuery request, CancellationToken cancellationToken)
    {
        var enabled = await settingRepository.GetByKeyAsync(
            SystemSetting.Keys.RegistrationEnabled, cancellationToken);

        // Mirrors RegisterCommandHandler: a missing row means open, not locked out.
        var isOpen = enabled is null
            || (bool.TryParse(enabled.Value, out var parsed) && parsed);

        if (isOpen)
            return Result.Ok(new RegistrationStatusDto(Open: true, InviteRequired: false));

        var inviteCode = await settingRepository.GetByKeyAsync(
            SystemSetting.Keys.InviteCode, cancellationToken);
        var hasCode = !string.IsNullOrWhiteSpace(inviteCode?.Value);

        return Result.Ok(new RegistrationStatusDto(Open: false, InviteRequired: hasCode));
    }
}
