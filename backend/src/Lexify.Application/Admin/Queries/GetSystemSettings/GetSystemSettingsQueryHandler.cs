using Lexify.Application.Admin.Dtos;
using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Admin.Queries.GetSystemSettings;

public sealed class GetSystemSettingsQueryHandler(ISystemSettingRepository settingRepository)
    : IRequestHandler<GetSystemSettingsQuery, Result<IReadOnlyList<SystemSettingDto>>>
{
    public async Task<Result<IReadOnlyList<SystemSettingDto>>> Handle(
        GetSystemSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await settingRepository.GetAllAsync(cancellationToken);

        IReadOnlyList<SystemSettingDto> dtos = settings
            .Select(s => new SystemSettingDto(s.Key, s.Value, s.ValueType, s.Description, s.UpdatedAt, s.UpdatedBy))
            .ToList();

        return Result.Ok(dtos);
    }
}
