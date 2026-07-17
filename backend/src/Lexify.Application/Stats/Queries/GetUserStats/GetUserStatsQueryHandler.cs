using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Stats.Queries.GetUserStats;

public sealed class GetUserStatsQueryHandler(
    IWordBlockRepository blockRepository,
    IWordRepository wordRepository,
    ITestAttemptRepository attemptRepository,
    IReviewLogRepository reviewLogRepository,
    IUserRepository userRepository) : IRequestHandler<GetUserStatsQuery, Result<UserStatsDto>>
{
    // Window for the dashboard streak. A 92-day look-back is plenty to render an active streak
    // without pulling a user's entire review history on every dashboard load.
    private const int StreakWindowDays = 92;

    public async Task<Result<UserStatsDto>> Handle(GetUserStatsQuery request, CancellationToken cancellationToken)
    {
        var since = DateTimeOffset.UtcNow.AddDays(-7);

        var (totalBlocks, totalWords) = await blockRepository.GetUserSummaryAsync(request.UserId, cancellationToken);

        // SQL counts instead of loading every due entity; the "new" share of today's queue is
        // capped by the user's remaining daily new-word budget.
        var dueCounts = await wordRepository.GetDueCountsAsync(request.UserId, cancellationToken);
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        var newLimit = user?.NewWordsPerDay ?? Domain.Entities.User.DefaultNewWordsPerDay;
        var utcDayStart = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        var introducedToday = await reviewLogRepository.CountNewWordsIntroducedSinceAsync(
            request.UserId, utcDayStart, cancellationToken);
        var dueNew = Math.Min(dueCounts.New, Math.Max(0, newLimit - introducedToday));

        var answersThisWeek = await attemptRepository.CountAnswersSinceAsync(request.UserId, since, cancellationToken);
        var testsThisWeek = await attemptRepository.CountCompletedSinceAsync(request.UserId, since, cancellationToken);

        var streakSince = DateTimeOffset.UtcNow.AddDays(-StreakWindowDays);
        var recentReviews = await reviewLogRepository.GetByUserSinceAsync(request.UserId, streakSince, cancellationToken);
        var activeDates = recentReviews
            .Select(l => DateOnly.FromDateTime(l.ReviewedAt.UtcDateTime))
            .ToHashSet();
        var currentStreak = StreakMath.CurrentStreak(activeDates, DateOnly.FromDateTime(DateTime.UtcNow));

        return Result<UserStatsDto>.Ok(new UserStatsDto(
            TotalBlocks: totalBlocks,
            TotalWords: totalWords,
            DueWordsCount: dueCounts.ReviewDue + dueNew,
            DueNewCount: dueNew,
            DueReviewCount: dueCounts.ReviewDue,
            WordsAnsweredThisWeek: answersThisWeek,
            TestsCompletedThisWeek: testsThisWeek,
            CurrentStreak: currentStreak));
    }
}
