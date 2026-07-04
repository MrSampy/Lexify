namespace Lexify.Application.Words.Commands.ImportWords;

public sealed record ImportWordItem(
    string Term,
    string Translation,
    string WordType,
    string? Notes,
    string? ExampleSentence,
    bool ConfidenceFlag,
    string? ConfidenceNote,
    int SortOrder = 0,
    IReadOnlyList<string>? AlternativeTranslations = null);
