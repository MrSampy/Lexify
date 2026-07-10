using System.Text.RegularExpressions;

namespace Lexify.Application.Tests.Services;

/// <summary>
/// Validates an LLM-generated example sentence before it's used in a quiz question or cached on a
/// Word. Sentences that fail are retried once (see GenerateTestJob) with the failure reason fed
/// back to the LLM; a word that fails twice falls back to a different question type instead.
/// </summary>
public static class FillSentenceValidator
{
    private const int MinWords = 5;
    private const int MaxWords = 24;
    private const int MinChars = 20;
    private const int MaxChars = 200;
    private static readonly char[] TerminalPunctuation = ['.', '!', '?'];

    public static FillSentenceCheck Check(string? sentence, string term)
    {
        if (string.IsNullOrWhiteSpace(sentence))
            return FillSentenceCheck.Fail("Sentence is empty.");

        var trimmed = sentence.Trim();

        if (trimmed.Length is < MinChars or > MaxChars)
            return FillSentenceCheck.Fail($"Sentence must be {MinChars}-{MaxChars} characters (was {trimmed.Length}).");

        var wordCount = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        if (wordCount is < MinWords or > MaxWords)
            return FillSentenceCheck.Fail($"Sentence must be {MinWords}-{MaxWords} words (was {wordCount}).");

        if (!TerminalPunctuation.Contains(trimmed[^1]))
            return FillSentenceCheck.Fail("Sentence must end with terminal punctuation (. ! or ?).");

        var occurrences = TermRegex(term).Matches(trimmed).Count;
        if (occurrences == 0)
            return FillSentenceCheck.Fail($"Sentence does not contain the term '{term}'.");
        if (occurrences > 1)
            return FillSentenceCheck.Fail($"Sentence uses the term '{term}' more than once.");

        return FillSentenceCheck.Ok(trimmed);
    }

    /// <summary>Replaces the (already-verified single) occurrence of the term with a quiz blank.</summary>
    public static string Blank(string sentence, string term) =>
        TermRegex(term).Replace(sentence, "___", 1);

    private static Regex TermRegex(string term) =>
        new($@"\b{Regex.Escape(term)}\b", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
}

public sealed record FillSentenceCheck(bool IsValid, string? ErrorMessage, string? Sentence)
{
    public static FillSentenceCheck Ok(string sentence) => new(true, null, sentence);
    public static FillSentenceCheck Fail(string error) => new(false, error, null);
}
