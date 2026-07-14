using Lexify.Application.Abstractions;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;

namespace Lexify.Application.AI.Services;

/// <summary>
/// Counts a user's AI calls for the current UTC day straight from ai_call_logs, which AIOrchestrator
/// already writes on every provider call — so there is no second counter to keep in sync (and no
/// Redis key that could quietly expire and hand out a free day of spend).
/// </summary>
public sealed class AiQuotaService(
    ISystemSettingRepository settingRepository,
    IAiCallLogRepository logRepository)
    : IAiQuotaService
{
    /// <summary>Used when the setting row is missing or unparseable, so a bad value can't mean "unlimited".</summary>
    private const int FallbackLimit = 50;

    public async Task<AiQuotaCheck> CheckAsync(Guid userId, CancellationToken ct = default)
    {
        var limit = await GetLimitAsync(ct);
        if (limit <= 0)
            return AiQuotaCheck.Unlimited;

        var used = await logRepository.CountByUserSinceAsync(userId, StartOfUtcDay(), ct);

        return new AiQuotaCheck(IsExceeded: used >= limit, Limit: limit, Used: used);
    }

    /// <summary>
    /// Midnight UTC today, with an explicit zero offset. Note that <c>DateTimeOffset.UtcNow.Date</c>
    /// would NOT do: it yields a DateTime whose Kind is Unspecified, and converting that back to a
    /// DateTimeOffset stamps it with the machine's local offset — which Npgsql then refuses to write
    /// to a timestamptz column. The quota window is a calendar day, so it resets at midnight rather
    /// than trailing 24h behind the user's last call.
    /// </summary>
    internal static DateTimeOffset StartOfUtcDay()
    {
        var now = DateTimeOffset.UtcNow;
        return new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
    }

    private async Task<int> GetLimitAsync(CancellationToken ct)
    {
        var setting = await settingRepository.GetByKeyAsync(
            SystemSetting.Keys.MaxAiCallsPerUserPerDay, ct);

        if (setting is null)
            return FallbackLimit;

        return int.TryParse(setting.Value, out var limit) ? limit : FallbackLimit;
    }
}
