using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Stats.Queries.GetActivity;

public sealed class GetActivityQueryHandler(IReviewLogRepository reviewLogRepository)
    : IRequestHandler<GetActivityQuery, Result<ActivityDto>>
{
    public async Task<Result<ActivityDto>> Handle(GetActivityQuery request, CancellationToken cancellationToken)
    {
        var days = Math.Clamp(request.Days, 1, 366);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var since = new DateTimeOffset(today.AddDays(-(days - 1)), TimeOnly.MinValue, TimeSpan.Zero);

        var logs = await reviewLogRepository.GetByUserSinceAsync(request.UserId, since, cancellationToken);

        var perDay = logs
            .GroupBy(l => DateOnly.FromDateTime(l.ReviewedAt.UtcDateTime))
            .ToDictionary(g => g.Key, g => g.Count());

        // Emit a row for every day in the window (zeros included) so the heatmap grid is dense.
        var series = new List<DailyReviewCount>(days);
        for (var i = 0; i < days; i++)
        {
            var date = today.AddDays(-(days - 1) + i);
            series.Add(new DailyReviewCount(date, perDay.GetValueOrDefault(date)));
        }

        var activeDates = perDay.Keys.ToHashSet();

        return Result.Ok(new ActivityDto(
            Days: series,
            CurrentStreak: StreakMath.CurrentStreak(activeDates, today),
            LongestStreak: StreakMath.LongestStreak(activeDates),
            TotalReviews: logs.Count));
    }
}
