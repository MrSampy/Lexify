using Lexify.Application.AI.Dtos;

namespace Lexify.Application.Abstractions;

public interface IAIProvider
{
    IAsyncEnumerable<string> StreamEnrichWordsAsync(
        IReadOnlyList<ParsedImportLine> lines,
        string targetLanguage,
        string nativeLanguage,
        CancellationToken ct = default);

    /// <summary>
    /// Generates one example sentence per requested word (batched call). Returns an empty list
    /// (never throws) when the LLM is unavailable or every configured provider fails — callers fall
    /// back to another question type for words that don't get a sentence.
    /// </summary>
    /// <param name="englishLevel">Learner's CEFR level (A1..C2); null = no difficulty targeting.</param>
    Task<IReadOnlyList<FillSentenceAtom>> GenerateFillSentencesAsync(
        IReadOnlyList<FillSentenceRequest> requests,
        string targetLanguage,
        string? englishLevel = null,
        CancellationToken ct = default);

    /// <summary>
    /// Generates one short monolingual definition per requested word (batched call). Returns an
    /// empty list (never throws) when the LLM is unavailable or every configured provider fails —
    /// callers fall back to another question type for words that don't get a definition.
    /// </summary>
    /// <param name="englishLevel">Learner's CEFR level (A1..C2); null = no difficulty targeting.</param>
    Task<IReadOnlyList<DefinitionAtom>> GenerateDefinitionsAsync(
        IReadOnlyList<DefinitionRequest> requests,
        string targetLanguage,
        string? englishLevel = null,
        CancellationToken ct = default);

    /// <summary>
    /// Generates plausible-but-wrong alternatives to a correct quiz answer, used only when the
    /// real-word distractor pool (DistractorPool) can't supply enough. Returns an empty list (never
    /// throws) when the LLM is unavailable.
    /// </summary>
    Task<IReadOnlyList<string>> GenerateFakeDistractorsAsync(
        string correctAnswer,
        int count,
        CancellationToken ct = default);

    Task<string?> SuggestBlockTitleAsync(
        IReadOnlyList<string> terms,
        string language,
        CancellationToken ct = default);

    /// <summary>
    /// Streams Lexi's next conversational reply in the target language, given the context (languages,
    /// CEFR level, scenario, target words) and the prior turns. Yields no chunks when every configured
    /// provider fails — the caller surfaces an error event rather than a broken reply.
    /// </summary>
    IAsyncEnumerable<string> StreamChatReplyAsync(
        ChatContext context,
        IReadOnlyList<ChatTurn> history,
        CancellationToken ct = default);

    /// <summary>
    /// After a conversation ends, judges for each target word whether it was used and used correctly,
    /// so the result can feed SM-2. Returns an empty list (never throws) when the LLM is unavailable —
    /// callers then leave the words' schedules untouched.
    /// </summary>
    Task<IReadOnlyList<WordUsageVerdict>> AnalyzeConversationAsync(
        IReadOnlyList<ChatTurn> history,
        IReadOnlyList<TargetWord> targetWords,
        string targetLanguage,
        CancellationToken ct = default);

    Task<bool> IsAvailableAsync(CancellationToken ct = default);
}
