using System.Text.Json;
using Lexify.Application.AI.Dtos;

namespace Lexify.Application.AI;

public static class AIResponseValidator
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Validates and reconciles an enrichment response against the batch that was sent. Unlike the
    /// old heuristic (recognize ≥50% of input lines by substring match), this is exact: every line
    /// id sent must come back, and for lines the deterministic parser already split, the parser's
    /// term/translation are treated as ground truth — any drift from the LLM is repaired rather
    /// than trusted (or used as a reason to reject the whole batch).
    /// </summary>
    public static AIValidationResult ValidateEnrichment(string rawJson, IReadOnlyList<ParsedImportLine> batch)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
            return AIValidationResult.Fail("AI returned an empty response.");

        // Some models keep rambling with chatty filler text after the JSON object is complete.
        // Extract just the first balanced {...} object and ignore anything trailing it.
        var jsonObject = ExtractFirstJsonObject(rawJson);
        if (jsonObject is null)
            return AIValidationResult.Fail("AI response does not contain a JSON object.");

        EnrichWordsAiResult? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<EnrichWordsAiResult>(jsonObject, JsonOptions);
        }
        catch (JsonException ex)
        {
            return AIValidationResult.Fail($"AI response is not valid JSON: {ex.Message}");
        }

        if (parsed is null || parsed.Words.Count == 0)
            return AIValidationResult.Fail("AI returned no words.");

        var byId = new Dictionary<int, EnrichedWordAiItem>();
        foreach (var word in parsed.Words)
            byId[word.Id] = word; // last one wins if the model duplicated an id

        var missingIds = batch.Select(l => l.Id).Where(id => !byId.ContainsKey(id)).ToList();
        if (missingIds.Count > 0)
            return AIValidationResult.Fail(
                $"AI response is missing entries for line id(s): {string.Join(", ", missingIds)}.");

        var items = new List<FormatWordItem>(batch.Count);
        foreach (var line in batch)
        {
            var ai = byId[line.Id];

            // For lines the parser already split, term/translation are ground truth — the LLM was
            // told to copy them exactly, but repair (don't reject) if it altered them anyway.
            var term = line.IsParsed ? line.Term! : ai.Term;

            // Exception: when the parser's translation is actually written in the target language
            // (same as the term) rather than the native language, the LLM re-translates it and moves
            // the original wrong-language word into synonyms. Only then do we trust the AI's translation
            // over the parser's for a parsed line.
            var translationWrongLanguage = line.IsParsed && ai.TranslationInTargetLanguage == true;
            var translation = (line.IsParsed && !translationWrongLanguage) ? line.Translation! : ai.Translation;

            var alternatives = MergeAlternatives(line.AlternativeTranslations, ai.AlternativeTranslations);

            // When we accepted a re-translation, the parser's original (wrong-language) translation
            // becomes a synonym alongside anything the AI already returned.
            var aiSynonyms = translationWrongLanguage
                ? (ai.Synonyms ?? []).Append(line.Translation!)
                : ai.Synonyms;
            var synonyms = MergeSynonyms(aiSynonyms, term);

            // A parenthetical note extracted deterministically from the user's own raw input is
            // ground truth, same as term/translation above — it wins over whatever the AI proposed.
            var notes = line.Notes ?? ai.Notes;

            items.Add(new FormatWordItem(
                Term: term,
                Translation: translation,
                WordType: ai.WordType,
                Notes: notes,
                ExampleSentence: ai.ExampleSentence,
                ConfidenceFlag: line.ConfidenceFlag,
                ConfidenceNote: ai.ConfidenceNote,
                AlternativeTranslations: alternatives,
                Synonyms: synonyms));
        }

        return AIValidationResult.Ok(new FormatWordsResult(items, parsed.SuggestedTitle));
    }

    private static List<string> MergeAlternatives(
        IReadOnlyList<string> deterministic, IReadOnlyList<string>? aiProvided)
    {
        var merged = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var alt in deterministic.Concat(aiProvided ?? []))
        {
            var trimmed = alt.Trim();
            if (trimmed.Length > 0 && seen.Add(trimmed))
                merged.Add(trimmed);
        }

        return merged;
    }

    /// <summary>Dedupes synonyms (case-insensitive), drops blanks, and drops any entry equal to the term.</summary>
    private static List<string> MergeSynonyms(IEnumerable<string>? synonyms, string term)
    {
        var merged = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var syn in synonyms ?? [])
        {
            var trimmed = syn.Trim();
            if (trimmed.Length > 0
                && !string.Equals(trimmed, term, StringComparison.OrdinalIgnoreCase)
                && seen.Add(trimmed))
                merged.Add(trimmed);
        }

        return merged;
    }

    /// <summary>
    /// Deterministic fallback used when every LLM retry for a batch fails: parsed lines still
    /// produce a usable FormatWordItem from parser output alone (no enrichment), so only truly
    /// unparseable raw lines are lost. See FormatWordsCommandHandler's graceful-degradation path.
    /// </summary>
    public static FormatWordsResult DegradeToParsedOnly(IReadOnlyList<ParsedImportLine> batch)
    {
        var items = batch
            .Where(l => l.IsParsed)
            .Select(l => new FormatWordItem(
                Term: l.Term!,
                Translation: l.Translation!,
                WordType: "word",
                Notes: l.Notes,
                ExampleSentence: null,
                ConfidenceFlag: l.ConfidenceFlag,
                ConfidenceNote: null,
                AlternativeTranslations: l.AlternativeTranslations,
                Synonyms: []))
            .ToList();

        return new FormatWordsResult(items, null);
    }

    /// <summary>Finds the first balanced top-level {...} object in text and ignores anything before/after it.</summary>
    public static string? ExtractFirstJsonObject(string text)
    {
        var start = text.IndexOf('{');
        if (start < 0) return null;

        var depth = 0;
        var inString = false;
        var escaped = false;

        for (var i = start; i < text.Length; i++)
        {
            var c = text[i];

            if (inString)
            {
                if (escaped) escaped = false;
                else if (c == '\\') escaped = true;
                else if (c == '"') inString = false;
                continue;
            }

            switch (c)
            {
                case '"': inString = true; break;
                case '{': depth++; break;
                case '}':
                    depth--;
                    if (depth == 0) return text[start..(i + 1)];
                    break;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the first balanced top-level [...] array in text. Some models (observed: Qwen2.5)
    /// return a bare JSON array of questions instead of the requested {"questions": [...]} object,
    /// even under response_format=json_object (which only enforces "valid JSON", not "a JSON object").
    /// </summary>
    public static string? ExtractFirstJsonArray(string text)
    {
        var start = text.IndexOf('[');
        if (start < 0) return null;

        var depth = 0;
        var inString = false;
        var escaped = false;

        for (var i = start; i < text.Length; i++)
        {
            var c = text[i];

            if (inString)
            {
                if (escaped) escaped = false;
                else if (c == '\\') escaped = true;
                else if (c == '"') inString = false;
                continue;
            }

            switch (c)
            {
                case '"': inString = true; break;
                case '[': depth++; break;
                case ']':
                    depth--;
                    if (depth == 0) return text[start..(i + 1)];
                    break;
            }
        }

        return null;
    }
}

public sealed record AIValidationResult(bool IsValid, string? ErrorMessage, FormatWordsResult? ParsedResult)
{
    public static AIValidationResult Ok(FormatWordsResult result) => new(true, null, result);
    public static AIValidationResult Fail(string error) => new(false, error, null);
}
