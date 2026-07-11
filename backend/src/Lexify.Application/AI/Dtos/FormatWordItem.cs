namespace Lexify.Application.AI.Dtos;

public sealed record FormatWordItem(
    string Term,
    string Translation,
    string WordType,
    string? Notes,
    string? ExampleSentence,
    bool ConfidenceFlag,
    string? ConfidenceNote,
    IReadOnlyList<string>? AlternativeTranslations = null,
    IReadOnlyList<string>? Synonyms = null);
