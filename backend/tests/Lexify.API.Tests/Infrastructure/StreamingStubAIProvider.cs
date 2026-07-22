using System.Text.Json;
using Lexify.Application.Abstractions;
using Lexify.Application.AI.Dtos;

namespace Lexify.API.Tests.Infrastructure;

/// <summary>Minimal AI provider stub that echoes back a well-formed enrichment response.</summary>
public sealed class StreamingStubAIProvider : IAIProvider
{
    public async IAsyncEnumerable<string> StreamEnrichWordsAsync(
        IReadOnlyList<ParsedImportLine> lines, string targetLanguage, string nativeLanguage,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        await Task.Yield();

        var words = lines.Select(line => new
        {
            id = line.Id,
            term = line.Term ?? line.RawLine,
            translation = line.Translation ?? "unknown",
            wordType = "word",
            alternativeTranslations = Array.Empty<string>(),
            notes = (string?)null,
            exampleSentence = (string?)null,
            confidenceNote = (string?)null
        });

        yield return JsonSerializer.Serialize(new { suggestedTitle = (string?)null, words });
    }

    public Task<IReadOnlyList<FillSentenceAtom>> GenerateFillSentencesAsync(
        IReadOnlyList<FillSentenceRequest> requests, string targetLanguage,
        string? englishLevel = null, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<FillSentenceAtom>>(
            requests.Select(r => new FillSentenceAtom(r.WordId, $"This is a sentence with {r.Term} in it.")).ToList());

    public Task<IReadOnlyList<DefinitionAtom>> GenerateDefinitionsAsync(
        IReadOnlyList<DefinitionRequest> requests, string targetLanguage,
        string? englishLevel = null, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<DefinitionAtom>>(
            requests.Select(r => new DefinitionAtom(r.WordId, "A thing used in tests to describe a concept clearly.")).ToList());

    public Task<IReadOnlyList<string>> GenerateFakeDistractorsAsync(
        string correctAnswer, int count, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<string>>([]);

    public Task<string?> SuggestBlockTitleAsync(
        IReadOnlyList<string> terms, string language, CancellationToken ct = default)
        => Task.FromResult<string?>(null);

    public async IAsyncEnumerable<string> StreamChatReplyAsync(
        ChatContext context, IReadOnlyList<ChatTurn> history,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        await Task.Yield();
        yield return "Hi there! Let's practise together.";
    }

    public Task<IReadOnlyList<WordUsageVerdict>> AnalyzeConversationAsync(
        IReadOnlyList<ChatTurn> history, IReadOnlyList<TargetWord> targetWords,
        string targetLanguage, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<WordUsageVerdict>>(
            targetWords.Select(w => new WordUsageVerdict(w.WordId, true, true, null)).ToList());

    public Task<bool> IsAvailableAsync(CancellationToken ct = default)
        => Task.FromResult(true);
}
