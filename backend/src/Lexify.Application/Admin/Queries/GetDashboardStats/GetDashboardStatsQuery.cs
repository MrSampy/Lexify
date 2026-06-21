using Lexify.Application.Admin.Dtos;
using Lexify.Application.Behaviors;
using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Admin.Queries.GetDashboardStats;

public sealed record GetDashboardStatsQuery : IRequest<Result<DashboardStatsDto>>, ICacheable
{
    public string CacheKey => "admin:dashboard:stats";
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(5);
}
