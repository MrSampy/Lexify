using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Stats.Queries.GetActivity;

/// <param name="Days">Size of the activity window, in days back from today (clamped 1–366).</param>
public sealed record GetActivityQuery(Guid UserId, int Days = 90)
    : IRequest<Result<ActivityDto>>;

public sealed record DailyReviewCount(DateOnly Date, int Count);

public sealed record ActivityDto(
    IReadOnlyList<DailyReviewCount> Days,
    int CurrentStreak,
    int LongestStreak,
    int TotalReviews);
