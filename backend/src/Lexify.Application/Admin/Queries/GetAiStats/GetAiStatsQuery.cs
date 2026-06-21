using Lexify.Application.Admin.Dtos;
using Lexify.Application.Behaviors;
using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Admin.Queries.GetAiStats;

public sealed record GetAiStatsQuery(int Hours = 24)
    : IRequest<Result<AiStatsDto>>, ICacheable
{
    public string CacheKey => $"admin:ai-stats:{Hours}";
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(5);
}
