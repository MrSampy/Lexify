using Lexify.Application.Admin.Dtos;
using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Admin.Queries.GetAiStatus;

public sealed class GetAiStatusQueryHandler(IAiCallLogRepository aiCallLogRepository)
    : IRequestHandler<GetAiStatusQuery, Result<IReadOnlyList<AiProviderStatusDto>>>
{
    public async Task<Result<IReadOnlyList<AiProviderStatusDto>>> Handle(
        GetAiStatusQuery request, CancellationToken cancellationToken)
    {
        var since = DateTimeOffset.UtcNow.AddMinutes(-request.WindowMinutes);
        var logs = await aiCallLogRepository.GetSinceAsync(since, cancellationToken);

        IReadOnlyList<AiProviderStatusDto> statuses = logs
            .GroupBy(l => l.Provider)
            .Select(g =>
            {
                var groupList = g.ToList();
                var total = groupList.Count;
                var successful = groupList.Count(l => l.Success);
                var successRate = total > 0 ? Math.Round((double)successful / total * 100, 1) : 0.0;
                var lastCallAt = groupList.Max(l => l.CreatedAt);

                var status = total == 0 ? "unknown"
                    : successRate >= 80.0 ? "healthy"
                    : "degraded";

                return new AiProviderStatusDto(
                    Provider: g.Key,
                    Status: status,
                    RecentCallCount: total,
                    RecentSuccessRatePercent: successRate,
                    LastCallAt: lastCallAt);
            })
            .OrderBy(s => s.Provider)
            .ToList();

        return Result.Ok(statuses);
    }
}
