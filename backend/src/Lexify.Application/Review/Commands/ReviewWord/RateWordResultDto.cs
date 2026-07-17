namespace Lexify.Application.Review.Commands.ReviewWord;

/// <summary>Post-review SM-2 state, so the UI can show "next review in X days" immediately.</summary>
public sealed record RateWordResultDto(
    int IntervalDays,
    DateTimeOffset NextReviewAt,
    double EaseFactor,
    int Repetitions,
    bool IsLeech);
