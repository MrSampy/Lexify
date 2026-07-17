using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Stats.Queries.GetUserStats;

public sealed record GetUserStatsQuery(Guid UserId) : IRequest<Result<UserStatsDto>>;

public sealed record UserStatsDto(
    int TotalBlocks,
    int TotalWords,
    int DueWordsCount,
    int DueNewCount,
    int DueReviewCount,
    int WordsAnsweredThisWeek,
    int TestsCompletedThisWeek,
    int CurrentStreak);
