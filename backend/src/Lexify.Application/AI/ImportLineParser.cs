using System.Text.RegularExpressions;
using Lexify.Application.AI.Dtos;

namespace Lexify.Application.AI;

/// <summary>
/// Splits raw multi-line vocabulary input into term/translation pairs without any LLM involvement.
/// Handles the overwhelming majority of real input (users consistently separate term and
/// translation with " - ", a dash variant, a tab, or ": "). Lines that don't match any of these
/// are left unparsed (Term = null) so the LLM can attempt them as free-form text instead.
/// </summary>
public static class ImportLineParser
{
    private static readonly char[] AlternativeSeparators = [',', '/'];
    private static readonly Regex ParentheticalRegex = new(@"\(([^()]*)\)", RegexOptions.Compiled);

    public static IReadOnlyList<ParsedImportLine> Parse(string rawText)
    {
        var lines = rawText
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(l => l.Length > 0)
            .ToList();

        var result = new List<ParsedImportLine>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
            result.Add(ParseLine(i + 1, lines[i]));
        return result;
    }

    private static ParsedImportLine ParseLine(int id, string rawLine)
    {
        var (termPart, translationPart) = SplitTermAndTranslation(rawLine);
        if (termPart is null || translationPart is null)
            return new ParsedImportLine(id, rawLine, null, null, [], false);

        var term = termPart.Trim();
        var translationRaw = translationPart.Trim();

        var confidenceFlag = translationRaw.EndsWith('?');
        if (confidenceFlag)
            translationRaw = translationRaw[..^1].TrimEnd();

        // Extract parenthetical notes before splitting alternatives so a comma/slash inside the
        // parentheses (e.g. "собака (розмовне, неформально)") isn't mistaken for an alt separator.
        var (translationWithoutNotes, notes) = ExtractParentheticalNotes(translationRaw);

        var (translation, alternatives) = SplitTranslationAlternatives(translationWithoutNotes);

        // Either side coming up empty (e.g. "term - " with nothing after the separator) means this
        // wasn't really a term/translation line — fall through to raw so the LLM has a shot at it.
        if (term.Length == 0 || translation.Length == 0)
            return new ParsedImportLine(id, rawLine, null, null, [], false);

        return new ParsedImportLine(id, rawLine, term, translation, alternatives, confidenceFlag, notes);
    }

    /// <summary>
    /// Strips any "(...)" groups from the translation and returns their contents as a note — e.g.
    /// "випадково(в контексті сну)" becomes ("випадково", "в контексті сну"). Multiple groups are
    /// joined with "; ". Runs before alternative-translation splitting so a comma/slash inside the
    /// parentheses is treated as part of the note, not as an alternative-translation separator.
    /// </summary>
    private static (string Stripped, string? Notes) ExtractParentheticalNotes(string text)
    {
        var matches = ParentheticalRegex.Matches(text);
        if (matches.Count == 0)
            return (text, null);

        var notes = string.Join("; ", matches.Select(m => m.Groups[1].Value.Trim()).Where(s => s.Length > 0));
        var stripped = Regex.Replace(ParentheticalRegex.Replace(text, ""), @"\s{2,}", " ").Trim();

        return (stripped, notes.Length > 0 ? notes : null);
    }

    /// <summary>
    /// Tries separators in priority order, most specific/least ambiguous first. Splits on the LAST
    /// occurrence of each separator so that a term itself containing a hyphen or colon (rare, but
    /// real for phrases like "father-in-law - тесть") still splits at the actual term/translation
    /// boundary rather than the first hyphen encountered.
    /// </summary>
    private static (string? Term, string? Translation) SplitTermAndTranslation(string line)
    {
        var idx = line.LastIndexOf(" - ", StringComparison.Ordinal);
        if (idx > 0) return (line[..idx], line[(idx + 3)..]);

        idx = line.LastIndexOf(" — ", StringComparison.Ordinal);
        if (idx > 0) return (line[..idx], line[(idx + 3)..]);

        idx = line.LastIndexOf(" – ", StringComparison.Ordinal);
        if (idx > 0) return (line[..idx], line[(idx + 3)..]);

        idx = line.IndexOf('\t');
        if (idx > 0) return (line[..idx], line[(idx + 1)..]);

        idx = line.LastIndexOf(": ", StringComparison.Ordinal);
        if (idx > 0) return (line[..idx], line[(idx + 2)..]);

        return (null, null);
    }

    private static (string First, IReadOnlyList<string> Alternatives) SplitTranslationAlternatives(string translation)
    {
        var parts = translation
            .Split(AlternativeSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(p => p.Length > 0)
            .ToList();

        return parts.Count == 0
            ? (translation, [])
            : (parts[0], parts.Skip(1).ToList());
    }
}
