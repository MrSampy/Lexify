using System.Diagnostics;
using System.Runtime.CompilerServices;
using Lexify.Application.Abstractions;
using Lexify.Application.AI.Dtos;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using Lexify.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexify.Infrastructure.AI;

/// <summary>
/// Tries each configured AI provider in order (config section "AiProviders") and falls back to the
/// next one when a provider fails. Every entry speaks the OpenAI-compatible chat completions protocol
/// (Ollama, Lemonade, OpenAI itself, etc. all support it), so a single client implementation is reused.
/// </summary>
public sealed partial class AIOrchestrator(
    IHttpClientFactory httpClientFactory,
    IOptions<List<AiProviderSettings>> providersOptions,
    AiProviderRotation rotation,
    IAiCallLogRepository logRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    ILogger<AIOrchestrator> logger)
    : IAIProvider
{
    private List<AiProviderSettings> Providers => providersOptions.Value;

    /// <summary>
    /// The provider order to try for one operation: same-endpoint keys are round-robined (spreading load
    /// so no single key hits its rate limit first), distinct endpoints keep their configured fallback
    /// order. Called once per operation — each call advances the rotation by one.
    /// </summary>
    private IReadOnlyList<AiProviderSettings> GetAttemptOrder() =>
        AiProviderOrdering.Order(Providers, rotation.Next());

    public async IAsyncEnumerable<string> StreamEnrichWordsAsync(
        IReadOnlyList<ParsedImportLine> lines,
        string targetLanguage,
        string nativeLanguage,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var settings in GetAttemptOrder())
        {
            var client = CreateClient(settings);
            var sw = Stopwatch.StartNew();
            HttpResponseMessage response;
            try
            {
                response = await client.OpenEnrichStreamAsync(lines, targetLanguage, nativeLanguage, ct);
            }
            catch (Exception ex)
            {
                LogProviderError(logger, ex, "StreamEnrichWords", settings.Name);
                await WriteLogAsync(AiCallLog.CallTypes.FormatWords, settings, sw, false, ex.Message, ct);
                continue;
            }

            using (response)
            {
                await foreach (var chunk in OpenAiCompatibleClient.EnumerateFormatStreamAsync(response, ct))
                    yield return chunk;
            }

            await WriteLogAsync(AiCallLog.CallTypes.FormatWords, settings, sw, true, null, ct);
            yield break;
        }

        LogAllProvidersFailed(logger, "StreamEnrichWords");
    }

    public async Task<IReadOnlyList<FillSentenceAtom>> GenerateFillSentencesAsync(
        IReadOnlyList<FillSentenceRequest> requests,
        string targetLanguage,
        string? englishLevel = null,
        CancellationToken ct = default)
    {
        foreach (var settings in GetAttemptOrder())
        {
            var client = CreateClient(settings);
            var sw = Stopwatch.StartNew();
            try
            {
                var result = await client.GenerateFillSentencesAsync(requests, targetLanguage, englishLevel, ct);
                await WriteLogAsync(AiCallLog.CallTypes.GenerateFillSentences, settings, sw, true, null, ct);
                return result;
            }
            catch (Exception ex)
            {
                LogProviderError(logger, ex, "GenerateFillSentences", settings.Name);
                await WriteLogAsync(AiCallLog.CallTypes.GenerateFillSentences, settings, sw, false, ex.Message, ct);
            }
        }

        LogAllProvidersFailed(logger, "GenerateFillSentences");
        return [];
    }

    public async Task<IReadOnlyList<DefinitionAtom>> GenerateDefinitionsAsync(
        IReadOnlyList<DefinitionRequest> requests,
        string targetLanguage,
        string? englishLevel = null,
        CancellationToken ct = default)
    {
        foreach (var settings in GetAttemptOrder())
        {
            var client = CreateClient(settings);
            var sw = Stopwatch.StartNew();
            try
            {
                var result = await client.GenerateDefinitionsAsync(requests, targetLanguage, englishLevel, ct);
                await WriteLogAsync(AiCallLog.CallTypes.GenerateDefinitions, settings, sw, true, null, ct);
                return result;
            }
            catch (Exception ex)
            {
                LogProviderError(logger, ex, "GenerateDefinitions", settings.Name);
                await WriteLogAsync(AiCallLog.CallTypes.GenerateDefinitions, settings, sw, false, ex.Message, ct);
            }
        }

        LogAllProvidersFailed(logger, "GenerateDefinitions");
        return [];
    }

    public async Task<IReadOnlyList<string>> GenerateFakeDistractorsAsync(
        string correctAnswer,
        int count,
        CancellationToken ct = default)
    {
        foreach (var settings in GetAttemptOrder())
        {
            var client = CreateClient(settings);
            var sw = Stopwatch.StartNew();
            try
            {
                var result = await client.GenerateFakeDistractorsAsync(correctAnswer, count, ct);
                await WriteLogAsync(AiCallLog.CallTypes.GenerateDistractors, settings, sw, true, null, ct);
                return result;
            }
            catch (Exception ex)
            {
                LogProviderError(logger, ex, "GenerateFakeDistractors", settings.Name);
                await WriteLogAsync(AiCallLog.CallTypes.GenerateDistractors, settings, sw, false, ex.Message, ct);
            }
        }

        LogAllProvidersFailed(logger, "GenerateFakeDistractors");
        return [];
    }

    public async Task<string?> SuggestBlockTitleAsync(
        IReadOnlyList<string> terms,
        string language,
        CancellationToken ct = default)
    {
        foreach (var settings in GetAttemptOrder())
        {
            var client = CreateClient(settings);
            var sw = Stopwatch.StartNew();
            try
            {
                var title = await client.SuggestBlockTitleAsync(terms, language, ct);
                await WriteLogAsync(AiCallLog.CallTypes.SuggestTitle, settings, sw, true, null, ct);
                if (title is not null) return title;
            }
            catch (Exception ex)
            {
                LogTitleSuggestionWarning(logger, ex, settings.Name);
                await WriteLogAsync(AiCallLog.CallTypes.SuggestTitle, settings, sw, false, ex.Message, ct);
            }
        }

        return null;
    }

    public async IAsyncEnumerable<string> StreamChatReplyAsync(
        ChatContext context,
        IReadOnlyList<ChatTurn> history,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var settings in GetAttemptOrder())
        {
            var client = CreateClient(settings);
            var sw = Stopwatch.StartNew();
            HttpResponseMessage response;
            try
            {
                response = await client.OpenChatStreamAsync(context, history, ct);
            }
            catch (Exception ex)
            {
                LogProviderError(logger, ex, "StreamChatReply", settings.Name);
                await WriteLogAsync(AiCallLog.CallTypes.Conversation, settings, sw, false, ex.Message, ct);
                continue;
            }

            using (response)
            {
                await foreach (var chunk in OpenAiCompatibleClient.EnumerateFormatStreamAsync(response, ct))
                    yield return chunk;
            }

            await WriteLogAsync(AiCallLog.CallTypes.Conversation, settings, sw, true, null, ct);
            yield break;
        }

        LogAllProvidersFailed(logger, "StreamChatReply");
    }

    public async Task<IReadOnlyList<WordUsageVerdict>> AnalyzeConversationAsync(
        IReadOnlyList<ChatTurn> history,
        IReadOnlyList<TargetWord> targetWords,
        string targetLanguage,
        CancellationToken ct = default)
    {
        foreach (var settings in GetAttemptOrder())
        {
            var client = CreateClient(settings);
            var sw = Stopwatch.StartNew();
            try
            {
                var result = await client.AnalyzeConversationAsync(history, targetWords, targetLanguage, ct);
                await WriteLogAsync(AiCallLog.CallTypes.AnalyzeConversation, settings, sw, true, null, ct);
                return result;
            }
            catch (Exception ex)
            {
                LogProviderError(logger, ex, "AnalyzeConversation", settings.Name);
                await WriteLogAsync(AiCallLog.CallTypes.AnalyzeConversation, settings, sw, false, ex.Message, ct);
            }
        }

        LogAllProvidersFailed(logger, "AnalyzeConversation");
        return [];
    }

    public Task<bool> IsAvailableAsync(CancellationToken ct = default) =>
        Task.FromResult(Providers.Count > 0);

    private OpenAiCompatibleClient CreateClient(AiProviderSettings settings)
    {
        var http = httpClientFactory.CreateClient($"ai:{settings.Name}");
        return new OpenAiCompatibleClient(http, settings, logger);
    }

    private async Task WriteLogAsync(
        string callType,
        AiProviderSettings settings,
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
                provider: settings.Name,
                model: settings.Model,
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

    [LoggerMessage(Level = LogLevel.Error, Message = "AI provider {ProviderName} failed on {Operation}")]
    private static partial void LogProviderError(ILogger logger, Exception ex, string operation, string providerName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "AI provider {ProviderName} failed to suggest block title")]
    private static partial void LogTitleSuggestionWarning(ILogger logger, Exception ex, string providerName);

    [LoggerMessage(Level = LogLevel.Error, Message = "All configured AI providers failed on {Operation}")]
    private static partial void LogAllProvidersFailed(ILogger logger, string operation);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to write AI call log")]
    private static partial void LogCallLogError(ILogger logger, Exception ex);
}
