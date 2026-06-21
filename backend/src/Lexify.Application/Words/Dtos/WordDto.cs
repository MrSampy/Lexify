namespace Lexify.Application.Words.Dtos;

public sealed record WordDto(
    Guid Id,
    Guid BlockId,
    string Term,
    string Translation,
    string WordType,
    string? Notes,
    string? ExampleSentence,
    bool ConfidenceFlag,
    string? ConfidenceNote,
    int SortOrder,
    DateTimeOffset CreatedAt,
    double EaseFactor,
    int IntervalDays,
    int Repetitions,
    DateTimeOffset NextReviewAt);
