using Lexify.Application.AI.Dtos;
using Lexify.Application.Words.Dtos;

namespace Lexify.Application.Abstractions;

public interface IAIProvider
{
    IAsyncEnumerable<string> StreamFormatWordsAsync(
        string rawText,
        string targetLanguage,
        string nativeLanguage,
        CancellationToken ct = default);

    /// <param name="englishLevel">Learner's CEFR level (A1..C2); null = no difficulty targeting.</param>
    Task<TestGenerationResult> GenerateTestQuestionsAsync(
        IReadOnlyList<WordDto> words,
        IReadOnlyList<string> questionTypes,
        int count,
        string? englishLevel = null,
        CancellationToken ct = default);

    Task<string?> SuggestBlockTitleAsync(
        IReadOnlyList<string> terms,
        string language,
        CancellationToken ct = default);

    Task<bool> IsAvailableAsync(CancellationToken ct = default);
}
