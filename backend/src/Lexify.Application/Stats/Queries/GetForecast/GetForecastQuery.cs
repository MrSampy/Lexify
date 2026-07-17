using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Stats.Queries.GetForecast;

/// <param name="Days">Forecast horizon in days (clamped to 1–30).</param>
public sealed record GetForecastQuery(Guid UserId, int Days = 14) : IRequest<Result<ForecastDto>>;

public sealed record ForecastDto(IReadOnlyList<DailyDueCount> Days);

/// <summary>Words scheduled for review on <paramref name="Date"/>; day 0 absorbs the overdue backlog.</summary>
public sealed record DailyDueCount(DateOnly Date, int Count);
