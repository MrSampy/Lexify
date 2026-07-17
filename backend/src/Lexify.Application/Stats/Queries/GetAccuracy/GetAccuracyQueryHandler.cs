using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Stats.Queries.GetAccuracy;

public sealed class GetAccuracyQueryHandler(IReviewLogRepository reviewLogRepository)
    : IRequestHandler<GetAccuracyQuery, Result<AccuracyDto>>
{
    // SM-2 quality >= 3 counts as a successful recall (0–2 are failed recalls).
    private const int RecallThreshold = 3;

    public async Task<Result<AccuracyDto>> Handle(GetAccuracyQuery request, CancellationToken cancellationToken)
    {
        var days = Math.Clamp(request.Days, 1, 366);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var since = new DateTimeOffset(today.AddDays(-(days - 1)), TimeOnly.MinValue, TimeSpan.Zero);

        var logs = await reviewLogRepository.GetByUserSinceAsync(request.UserId, since, cancellationToken);

        // Only days with activity carry a data point — an accuracy line shouldn't plot 0% on idle days.
        var series = logs
            .GroupBy(l => DateOnly.FromDateTime(l.ReviewedAt.UtcDateTime))
            .OrderBy(g => g.Key)
            .Select(g => new DailyAccuracy(
                Date: g.Key,
                Total: g.Count(),
                Correct: g.Count(l => l.Quality >= RecallThreshold)))
            .ToList();

        return Result.Ok(new AccuracyDto(series));
    }
}
