using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Stats.Queries.GetProblemWords;

public sealed record GetProblemWordsQuery(Guid UserId, int Limit = 20)
    : IRequest<Result<IReadOnlyList<ProblemWordDto>>>;

/// <summary>A word the user keeps forgetting (leech or manually flagged), worst first.</summary>
public sealed record ProblemWordDto(
    Guid WordId,
    Guid BlockId,
    string BlockTitle,
    string Term,
    string Translation,
    int LapseCount,
    double EaseFactor,
    int IntervalDays,
    DateTimeOffset NextReviewAt,
    bool ConfidenceFlag);
