using Lexify.Application.Abstractions;
using Lexify.Application.AI.Dtos;
using Lexify.Application.Words.Dtos;

namespace Lexify.API.Tests.Infrastructure;

/// <summary>Minimal AI provider stub that yields a single well-formed JSON response.</summary>
public sealed class StreamingStubAIProvider : IAIProvider
{
    private const string StubJson =
        """{"words":[{"term":"hello","translation":"привіт","wordType":"word","notes":null,"exampleSentence":null}],"suggestedTitle":null}""";

    public async IAsyncEnumerable<string> StreamFormatWordsAsync(
        string rawText, string targetLanguage, string nativeLanguage,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        await Task.Yield();
        yield return StubJson;
    }

    public Task<TestGenerationResult> GenerateTestQuestionsAsync(
        IReadOnlyList<WordDto> words, IReadOnlyList<string> questionTypes,
        int count, CancellationToken ct = default)
        => Task.FromResult(new TestGenerationResult([]));

    public Task<string?> SuggestBlockTitleAsync(
        IReadOnlyList<string> terms, string language, CancellationToken ct = default)
        => Task.FromResult<string?>(null);

    public Task<bool> IsAvailableAsync(CancellationToken ct = default)
        => Task.FromResult(true);
}
