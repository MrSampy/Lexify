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
    DateTimeOffset NextReviewAt,
    IReadOnlyList<string>? AlternativeTranslations = null,
    IReadOnlyList<string>? Synonyms = null,
    // Populated only where the consumer needs it (e.g. review cards for TTS); word lists
    // already know their block's language.
    short? LanguageId = null);
