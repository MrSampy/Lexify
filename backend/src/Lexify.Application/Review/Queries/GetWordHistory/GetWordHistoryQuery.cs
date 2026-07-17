using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Review.Queries.GetWordHistory;

public sealed record GetWordHistoryQuery(Guid UserId, Guid WordId, int Limit = 50)
    : IRequest<Result<IReadOnlyList<WordReviewEntryDto>>>;

/// <summary>One past review of a word, with the SM-2 state it produced.</summary>
public sealed record WordReviewEntryDto(
    int Quality,
    string Source,
    double EaseFactorAfter,
    int IntervalDaysAfter,
    DateTimeOffset ReviewedAt);
