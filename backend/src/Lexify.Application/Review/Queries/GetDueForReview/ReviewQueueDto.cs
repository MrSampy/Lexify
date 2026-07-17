using Lexify.Application.Words.Dtos;

namespace Lexify.Application.Review.Queries.GetDueForReview;

/// <summary>
/// A review session queue with its composition: how many words are brand new vs scheduled
/// reviews, plus the user's daily new-word budget so the UI can explain what was (not) included.
/// </summary>
public sealed record ReviewQueueDto(
    IReadOnlyList<WordDto> Words,
    int NewCount,
    int ReviewCount,
    int NewLimit,
    int NewIntroducedToday);
