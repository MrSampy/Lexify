using System.Diagnostics;
using System.Runtime.CompilerServices;
using Lexify.Application.Abstractions;
using Lexify.Application.AI.Dtos;
using Lexify.Application.Words.Dtos;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Lexify.Infrastructure.AI;

public sealed partial class AIOrchestrator(
    OllamaProvider ollama,
    OpenAIProvider openai,
    IAiCallLogRepository logRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    ILogger<AIOrchestrator> logger)
    : IAIProvider
{
    public async IAsyncEnumerable<string> StreamFormatWordsAsync(
        string rawText,
        string targetLanguage,
        string nativeLanguage,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var useOllama = await ollama.IsAvailableAsync(ct);
        var provider = useOllama ? (IAIProvider)ollama : openai;
        var providerName = useOllama ? AiCallLog.Providers.Ollama : AiCallLog.Providers.OpenAI;

        if (!useOllama)
            LogFallback(logger, "StreamFormatWords");

        var sw = Stopwatch.StartNew();

        IAsyncEnumerable<string> source;
        try
        {
            source = provider.StreamFormatWordsAsync(rawText, targetLanguage, nativeLanguage, ct);
        }
        catch (Exception ex)
        {
            LogProviderError(logger, ex, "StreamFormatWords");
            await WriteLogAsync(AiCallLog.CallTypes.FormatWords, providerName, sw, false, ex.Message, ct);
            yield break;
        }

        var success = true;
        string? errorMessage = null;

        await foreach (var chunk in source.WithCancellation(ct))
            yield return chunk;

        await WriteLogAsync(AiCallLog.CallTypes.FormatWords, providerName, sw, success, errorMessage, ct);
    }

    public async Task<TestGenerationResult> GenerateTestQuestionsAsync(
        IReadOnlyList<WordDto> words,
        IReadOnlyList<string> questionTypes,
        int count,
        CancellationToken ct = default)
    {
        var useOllama = await ollama.IsAvailableAsync(ct);
        var provider = useOllama ? (IAIProvider)ollama : openai;
        var providerName = useOllama ? AiCallLog.Providers.Ollama : AiCallLog.Providers.OpenAI;

        var sw = Stopwatch.StartNew();
        try
        {
            var result = await provider.GenerateTestQuestionsAsync(words, questionTypes, count, ct);
            await WriteLogAsync(AiCallLog.CallTypes.GenerateTest, providerName, sw, true, null, ct);
            return result;
        }
        catch (Exception ex)
        {
            LogProviderError(logger, ex, "GenerateTestQuestions");
            await WriteLogAsync(AiCallLog.CallTypes.GenerateTest, providerName, sw, false, ex.Message, ct);
            throw;
        }
    }

    public async Task<string?> SuggestBlockTitleAsync(
        IReadOnlyList<string> terms,
        string language,
        CancellationToken ct = default)
    {
        var useOllama = await ollama.IsAvailableAsync(ct);
        var provider = useOllama ? (IAIProvider)ollama : openai;
        var providerName = useOllama ? AiCallLog.Providers.Ollama : AiCallLog.Providers.OpenAI;

        var sw = Stopwatch.StartNew();
        try
        {
            var title = await provider.SuggestBlockTitleAsync(terms, language, ct);
            await WriteLogAsync(AiCallLog.CallTypes.SuggestTitle, providerName, sw, true, null, ct);
            return title;
        }
        catch (Exception ex)
        {
            LogTitleSuggestionWarning(logger, ex);
            await WriteLogAsync(AiCallLog.CallTypes.SuggestTitle, providerName, sw, false, ex.Message, ct);
            return null;
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default) =>
        await ollama.IsAvailableAsync(ct) || await openai.IsAvailableAsync(ct);

    private async Task WriteLogAsync(
        string callType,
        string providerName,
        Stopwatch sw,
        bool success,
        string? errorMessage,
        CancellationToken ct)
    {
        sw.Stop();
        try
        {
            var log = new AiCallLog(
                userId: currentUser.IsAuthenticated ? currentUser.UserId : null,
                callType: callType,
                provider: providerName,
                model: providerName == AiCallLog.Providers.Ollama ? "qwen3:8b" : "gpt-4o-mini",
                durationMs: (int)sw.ElapsedMilliseconds,
                success: success,
                errorMessage: errorMessage);

            await logRepository.AddAsync(log, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            LogCallLogError(logger, ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Ollama unavailable, falling back to OpenAI for {Operation}")]
    private static partial void LogFallback(ILogger logger, string operation);

    [LoggerMessage(Level = LogLevel.Error, Message = "AI provider failed on {Operation}")]
    private static partial void LogProviderError(ILogger logger, Exception ex, string operation);

    [LoggerMessage(Level = LogLevel.Warning, Message = "AI provider failed to suggest block title")]
    private static partial void LogTitleSuggestionWarning(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to write AI call log")]
    private static partial void LogCallLogError(ILogger logger, Exception ex);
}
