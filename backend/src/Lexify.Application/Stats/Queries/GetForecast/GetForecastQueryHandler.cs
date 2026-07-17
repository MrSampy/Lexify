using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Stats.Queries.GetForecast;

public sealed class GetForecastQueryHandler(IWordRepository wordRepository)
    : IRequestHandler<GetForecastQuery, Result<ForecastDto>>
{
    public async Task<Result<ForecastDto>> Handle(
        GetForecastQuery request, CancellationToken cancellationToken)
    {
        var days = Math.Clamp(request.Days, 1, 30);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var horizonEnd = new DateTimeOffset(
            today.AddDays(days).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var times = await wordRepository.GetScheduledReviewTimesAsync(
            request.UserId, horizonEnd, cancellationToken);

        // Bucket in memory (dense series, zeros filled) — the per-user volume is bounded and this
        // avoids date-truncation translation quirks with DateTimeOffset in SQL. Everything overdue
        // collapses into day 0: it's all due "today".
        var counts = new int[days];
        foreach (var time in times)
        {
            var date = DateOnly.FromDateTime(time.UtcDateTime);
            var offset = Math.Max(0, date.DayNumber - today.DayNumber);
            counts[offset]++;
        }

        var series = Enumerable.Range(0, days)
            .Select(i => new DailyDueCount(today.AddDays(i), counts[i]))
            .ToList();

        return Result.Ok(new ForecastDto(series));
    }
}
