using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Stats.Queries.GetUserStats;

public sealed class GetUserStatsQueryHandler(
    IWordBlockRepository blockRepository,
    IWordRepository wordRepository,
    ITestAttemptRepository attemptRepository) : IRequestHandler<GetUserStatsQuery, Result<UserStatsDto>>
{
    public async Task<Result<UserStatsDto>> Handle(GetUserStatsQuery request, CancellationToken cancellationToken)
    {
        var since = DateTimeOffset.UtcNow.AddDays(-7);

        var (totalBlocks, totalWords) = await blockRepository.GetUserSummaryAsync(request.UserId, cancellationToken);
        var dueWords = await wordRepository.GetDueForReviewAsync(request.UserId, limit: 9999, cancellationToken);
        var answersThisWeek = await attemptRepository.CountAnswersSinceAsync(request.UserId, since, cancellationToken);
        var testsThisWeek = await attemptRepository.CountCompletedSinceAsync(request.UserId, since, cancellationToken);

        return Result<UserStatsDto>.Ok(new UserStatsDto(
            TotalBlocks: totalBlocks,
            TotalWords: totalWords,
            DueWordsCount: dueWords.Count,
            WordsAnsweredThisWeek: answersThisWeek,
            TestsCompletedThisWeek: testsThisWeek));
    }
}
