namespace Lexify.API.Requests.Words;

public sealed record ImportWordsRequest(IReadOnlyList<ImportWordItemRequest> Words);

public sealed record ImportWordItemRequest(
    string Term,
    string Translation,
    string WordType,
    string? Notes,
    string? ExampleSentence,
    bool ConfidenceFlag,
    string? ConfidenceNote,
    int SortOrder = 0);
