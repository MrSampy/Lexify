using Lexify.Application.Admin.Dtos;
using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Admin.Queries.GetAiStats;

public sealed class GetAiStatsQueryHandler(IAiCallLogRepository aiCallLogRepository)
    : IRequestHandler<GetAiStatsQuery, Result<AiStatsDto>>
{
    public async Task<Result<AiStatsDto>> Handle(GetAiStatsQuery request, CancellationToken cancellationToken)
    {
        var since = DateTimeOffset.UtcNow.AddHours(-request.Hours);
        var logs = await aiCallLogRepository.GetSinceAsync(since, cancellationToken);

        var total = logs.Count;
        var successful = logs.Count(l => l.Success);
        var failed = total - successful;
        var errorRate = total > 0 ? Math.Round((double)failed / total * 100, 2) : 0.0;
        var avgDuration = total > 0 ? Math.Round(logs.Average(l => l.DurationMs), 2) : 0.0;

        // "fallback" = calls handled by anything other than the most-used (primary) configured provider
        var primaryProvider = logs
            .GroupBy(l => l.Provider)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault();
        var fallbackCount = logs.Count(l => l.Provider != primaryProvider);

        var byCallType = logs
            .GroupBy(l => l.CallType)
            .Select(g =>
            {
                var groupList = g.ToList();
                return new AiCallTypeStatDto(
                    CallType: g.Key,
                    Count: groupList.Count,
                    AvgDurationMs: Math.Round(groupList.Average(l => l.DurationMs), 2),
                    ErrorCount: groupList.Count(l => !l.Success));
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        var byProvider = logs
            .GroupBy(l => l.Provider)
            .Select(g =>
            {
                var groupList = g.ToList();
                return new AiProviderStatDto(
                    Provider: g.Key,
                    Count: groupList.Count,
                    AvgDurationMs: Math.Round(groupList.Average(l => l.DurationMs), 2),
                    ErrorCount: groupList.Count(l => !l.Success));
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        var dto = new AiStatsDto(
            TotalCalls: total,
            SuccessfulCalls: successful,
            FailedCalls: failed,
            ErrorRatePercent: errorRate,
            FallbackCount: fallbackCount,
            AverageResponseTimeMs: avgDuration,
            ByCallType: byCallType,
            ByProvider: byProvider);

        return Result.Ok(dto);
    }
}
