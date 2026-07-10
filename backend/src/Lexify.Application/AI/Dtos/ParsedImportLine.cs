namespace Lexify.Application.AI.Dtos;

/// <summary>
/// Result of deterministically splitting one raw input line into term/translation before the LLM
/// ever sees it. Lines that don't match a known separator pattern are passed through as "raw" —
/// the LLM extracts and translates those itself.
/// </summary>
public sealed record ParsedImportLine(
    int Id,
    string RawLine,
    string? Term,
    string? Translation,
    IReadOnlyList<string> AlternativeTranslations,
    bool ConfidenceFlag)
{
    public bool IsParsed => Term is not null;
}
