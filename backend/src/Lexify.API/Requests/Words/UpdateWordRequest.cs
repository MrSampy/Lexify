namespace Lexify.API.Requests.Words;

public sealed record UpdateWordRequest(
    string Translation,
    string? Notes,
    string? ExampleSentence,
    bool ConfidenceFlag,
    string? ConfidenceNote,
    IReadOnlyList<string>? AlternativeTranslations = null);
