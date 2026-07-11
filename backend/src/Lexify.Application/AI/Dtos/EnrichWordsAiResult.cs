namespace Lexify.Application.AI.Dtos;

/// <summary>Raw shape of the LLM's enrichment response — mirrors AiJsonSchemas.EnrichWordsResult.</summary>
public sealed record EnrichWordsAiResult(
    string? SuggestedTitle,
    IReadOnlyList<EnrichedWordAiItem> Words);

/// <summary>
/// One enriched entry, keyed by the same "id" that was sent in the request so the deterministic
/// parser output and the LLM's enrichment can be matched back up (see AIResponseValidator.ValidateEnrichment).
/// </summary>
public sealed record EnrichedWordAiItem(
    int Id,
    string Term,
    string Translation,
    string WordType,
    IReadOnlyList<string>? AlternativeTranslations,
    string? Notes,
    string? ExampleSentence,
    string? ConfidenceNote,
    IReadOnlyList<string>? Synonyms = null,
    bool? TranslationInTargetLanguage = null);
