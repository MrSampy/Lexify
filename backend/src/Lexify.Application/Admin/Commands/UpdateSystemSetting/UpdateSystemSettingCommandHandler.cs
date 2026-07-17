using System.Text.Json;
using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Admin.Commands.UpdateSystemSetting;

public sealed class UpdateSystemSettingCommandHandler(
    ISystemSettingRepository settingRepository,
    IAuditService auditService,
    ICacheService cacheService,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateSystemSettingCommand, Result>
{
    public async Task<Result> Handle(UpdateSystemSettingCommand request, CancellationToken cancellationToken)
    {
        var setting = await settingRepository.GetByKeyAsync(request.Key, cancellationToken);
        if (setting is null)
            return Result.NotFound($"Setting '{request.Key}' not found.");

        if (!IsValueValid(request.Value, setting.ValueType))
            return Result.Failure($"Value '{request.Value}' is not valid for type '{setting.ValueType}'.");

        var oldValue = setting.Value;
        setting.Update(request.Value, currentUser.UserId);
        await settingRepository.UpdateAsync(setting, cancellationToken);

        await auditService.LogAsync(
            "update_system_setting", "SystemSetting", request.Key,
            oldValueJson: ToJsonAuditValue(oldValue, setting.ValueType),
            newValueJson: ToJsonAuditValue(request.Value, setting.ValueType),
            ct: cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cacheService.RemoveAsync("admin:settings", cancellationToken);

        return Result.Ok();
    }

    // AuditLog.OldValue/NewValue are jsonb columns — "string"-typed settings aren't
    // valid JSON on their own (unquoted), so they need to be JSON-encoded first.
    private static string ToJsonAuditValue(string value, string valueType) =>
        valueType == "string" ? JsonSerializer.Serialize(value) : value;

    private static bool IsValueValid(string value, string valueType) => valueType switch
    {
        "int" => int.TryParse(value, out _),
        "bool" => bool.TryParse(value, out _),
        "decimal" => decimal.TryParse(value, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out _),
        "double" => double.TryParse(value, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out _),
        "json" => IsValidJson(value),
        _ => true  // "string" and unknown types — any value is accepted
    };

    private static bool IsValidJson(string value)
    {
        try
        {
            JsonDocument.Parse(value);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
