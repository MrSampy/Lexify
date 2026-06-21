namespace Lexify.API.Requests.Words;

public sealed record CreateWordRequest(
    string Term,
    string Translation,
    string WordType,
    string? Notes,
    string? ExampleSentence,
    int SortOrder = 0);
