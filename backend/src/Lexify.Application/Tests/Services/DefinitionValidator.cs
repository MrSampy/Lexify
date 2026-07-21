using System.Text.RegularExpressions;

namespace Lexify.Application.Tests.Services;

/// <summary>
/// Validates an LLM-generated monolingual definition before it's used in a definition-match
/// question or cached on a Word. Definitions that fail are retried once (see GenerateTestJob) with
/// the failure reason fed back to the LLM; a word that fails twice falls back to a different
/// question type instead. The definition must not give the answer away: it may contain neither the
/// term (word-boundary match) nor the translation (substring match).
/// </summary>
public static class DefinitionValidator
{
    private const int MinWords = 4;
    private const int MaxWords = 30;
    private const int MinChars = 20;
    private const int MaxChars = 180;

    public static DefinitionCheck Check(string? definition, string term, string translation)
    {
        if (string.IsNullOrWhiteSpace(definition))
            return DefinitionCheck.Fail("Definition is empty.");

        var trimmed = definition.Trim();

        if (trimmed.Length is < MinChars or > MaxChars)
            return DefinitionCheck.Fail($"Definition must be {MinChars}-{MaxChars} characters (was {trimmed.Length}).");

        var wordCount = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        if (wordCount is < MinWords or > MaxWords)
            return DefinitionCheck.Fail($"Definition must be {MinWords}-{MaxWords} words (was {wordCount}).");

        if (TermRegex(term).IsMatch(trimmed))
            return DefinitionCheck.Fail($"Definition must not contain the term '{term}'.");

        if (trimmed.Contains(translation, StringComparison.OrdinalIgnoreCase))
            return DefinitionCheck.Fail($"Definition must not contain the translation '{translation}'.");

        return DefinitionCheck.Ok(trimmed);
    }

    private static Regex TermRegex(string term) =>
        new($@"\b{Regex.Escape(term)}\b", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
}

public sealed record DefinitionCheck(bool IsValid, string? ErrorMessage, string? Definition)
{
    public static DefinitionCheck Ok(string definition) => new(true, null, definition);
    public static DefinitionCheck Fail(string error) => new(false, error, null);
}
