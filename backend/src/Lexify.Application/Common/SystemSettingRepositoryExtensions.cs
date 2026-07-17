using Lexify.Domain.Repositories;

namespace Lexify.Application.Common;

/// <summary>Typed reads of runtime system settings, with a fallback when the row is missing or unparseable.</summary>
public static class SystemSettingRepositoryExtensions
{
    public static async Task<int> GetIntAsync(
        this ISystemSettingRepository repository, string key, int fallback, CancellationToken ct = default)
    {
        var setting = await repository.GetByKeyAsync(key, ct);
        return setting is not null && int.TryParse(setting.Value, out var value) ? value : fallback;
    }

    public static async Task<bool> GetBoolAsync(
        this ISystemSettingRepository repository, string key, bool fallback, CancellationToken ct = default)
    {
        var setting = await repository.GetByKeyAsync(key, ct);
        return setting is not null && bool.TryParse(setting.Value, out var value) ? value : fallback;
    }
}
