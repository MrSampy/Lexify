using Lexify.Application.Admin.Dtos;
using Lexify.Application.Behaviors;
using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Admin.Queries.GetAiCallsChart;

public sealed record GetAiCallsChartQuery(int Hours = 24)
    : IRequest<Result<IReadOnlyList<AiCallDataPointDto>>>, ICacheable
{
    public string CacheKey => $"admin:chart:ai-calls:{Hours}";
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(5);
}
