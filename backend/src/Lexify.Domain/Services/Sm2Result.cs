namespace Lexify.Domain.Services;

public sealed record Sm2Result(
    double EaseFactor,
    int IntervalDays,
    int Repetitions,
    DateTimeOffset NextReviewAt);
