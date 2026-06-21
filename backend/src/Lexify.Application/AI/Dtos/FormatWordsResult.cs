namespace Lexify.Application.AI.Dtos;

public sealed record FormatWordsResult(
    IReadOnlyList<FormatWordItem> Words,
    string? SuggestedTitle);
