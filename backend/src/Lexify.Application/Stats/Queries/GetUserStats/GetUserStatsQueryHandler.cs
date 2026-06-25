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

        var summaryTask = blockRepository.GetUserSummaryAsync(request.UserId, cancellationToken);
        var dueTask = wordRepository.GetDueForReviewAsync(request.UserId, limit: 9999, cancellationToken);
        var answersTask = attemptRepository.CountAnswersSinceAsync(request.UserId, since, cancellationToken);
        var testsTask = attemptRepository.CountCompletedSinceAsync(request.UserId, since, cancellationToken);

        await Task.WhenAll(summaryTask, dueTask, answersTask, testsTask);

        var (totalBlocks, totalWords) = await summaryTask;

        return Result<UserStatsDto>.Ok(new UserStatsDto(
            TotalBlocks: totalBlocks,
            TotalWords: totalWords,
            DueWordsCount: (await dueTask).Count,
            WordsAnsweredThisWeek: await answersTask,
            TestsCompletedThisWeek: await testsTask));
    }
}
