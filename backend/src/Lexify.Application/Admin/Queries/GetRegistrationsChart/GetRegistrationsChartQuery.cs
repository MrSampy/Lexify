using Lexify.Application.Admin.Dtos;
using Lexify.Application.Behaviors;
using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Admin.Queries.GetRegistrationsChart;

public sealed record GetRegistrationsChartQuery(int Days = 30)
    : IRequest<Result<IReadOnlyList<RegistrationDataPointDto>>>, ICacheable
{
    public string CacheKey => $"admin:chart:registrations:{Days}";
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(5);
}
