using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Lexify.Application.Abstractions;
using Lexify.Application.AI.Dtos;
using Lexify.Application.Words.Dtos;
using Lexify.Infrastructure.AI.Models;
using Lexify.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexify.Infrastructure.AI;

public sealed partial class OpenAIProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<OpenAISettings> options,
    ILogger<OpenAIProvider> logger)
    : IAIProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly OpenAISettings _settings = options.Value;

    public async IAsyncEnumerable<string> StreamFormatWordsAsync(
        string rawText,
        string targetLanguage,
        string nativeLanguage,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var request = new OpenAIChatRequest
        {
            Model = _settings.Model,
            Messages =
            [
                new OpenAIMessage { Role = "system", Content = BuildFormatSystemPrompt() },
                new OpenAIMessage { Role = "user", Content = BuildFormatUserPrompt(rawText, targetLanguage, nativeLanguage) }
            ],
            Temperature = 0.1,
            Stream = true
        };

        var http = httpClientFactory.CreateClient("openai");
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("Accept", "text/event-stream");

        using var response = await http.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (!line.StartsWith("data: ", StringComparison.Ordinal)) continue;

            var data = line["data: ".Length..];
            if (data == "[DONE]") break;

            OpenAIStreamChunk? chunk;
            try { chunk = JsonSerializer.Deserialize<OpenAIStreamChunk>(data, JsonOptions); }
            catch { continue; }

            if (chunk?.Choices?[0]?.Delta?.Content is { Length: > 0 } content)
                yield return content;
        }
    }

    public async Task<TestGenerationResult> GenerateTestQuestionsAsync(
        IReadOnlyList<WordDto> words,
        IReadOnlyList<string> questionTypes,
        int count,
        CancellationToken ct = default)
    {
        var wordsJson = JsonSerializer.Serialize(words.Select(w => new { w.Term, w.Translation, w.WordType }));
        var request = new OpenAIChatRequest
        {
            Model = _settings.Model,
            Messages =
            [
                new OpenAIMessage { Role = "system", Content = BuildTestSystemPrompt(count, questionTypes) },
                new OpenAIMessage { Role = "user", Content = wordsJson }
            ],
            Temperature = 0.6,
            Stream = false
        };

        var http = httpClientFactory.CreateClient("openai");
        using var response = await http.PostAsJsonAsync("/v1/chat/completions", request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OpenAIResponse>(JsonOptions, ct);
        var json = result?.Choices?[0]?.Message?.Content ?? "{}";

        return JsonSerializer.Deserialize<TestGenerationResult>(json, JsonOptions)
               ?? new TestGenerationResult([]);
    }

    public async Task<string?> SuggestBlockTitleAsync(
        IReadOnlyList<string> terms,
        string language,
        CancellationToken ct = default)
    {
        var request = new OpenAIChatRequest
        {
            Model = _settings.Model,
            Messages =
            [
                new OpenAIMessage
                {
                    Role = "system",
                    Content = $"Return ONLY a short title (2-5 words) for a {language} vocabulary block. No explanation."
                },
                new OpenAIMessage { Role = "user", Content = string.Join(", ", terms.Take(20)) }
            ],
            Temperature = 0.3,
            Stream = false
        };

        var http = httpClientFactory.CreateClient("openai");
        try
        {
            using var response = await http.PostAsJsonAsync("/v1/chat/completions", request, ct);
            if (!response.IsSuccessStatusCode) return null;

            var result = await response.Content.ReadFromJsonAsync<OpenAIResponse>(JsonOptions, ct);
            return result?.Choices?[0]?.Message?.Content?.Trim();
        }
        catch (Exception ex)
        {
            LogTitleSuggestionWarning(logger, ex);
            return null;
        }
    }

    public Task<bool> IsAvailableAsync(CancellationToken ct = default) =>
        Task.FromResult(!string.IsNullOrEmpty(_settings.ApiKey));

    private static string BuildFormatSystemPrompt() =>
        """
        Return ONLY a valid JSON object — no markdown, no explanation.
        Schema:
        {
          "suggestedTitle": "string or null",
          "words": [
            {
              "term": "string",
              "translation": "string",
              "wordType": "word|phrase|idiom|expression",
              "notes": "string or null",
              "exampleSentence": "string or null",
              "confidenceFlag": false,
              "confidenceNote": "string or null"
            }
          ]
        }
        Rules:
        - Parse each non-empty line as one entry
        - confidenceFlag=true if translation is uncertain (user marked with ?)
        - Suggest a 2-4 word block title based on the content theme
        """;

    private static string BuildFormatUserPrompt(string rawText, string targetLanguage, string nativeLanguage) =>
        $"Input language: {targetLanguage}\nTranslation language: {nativeLanguage}\n\n{rawText}";

    private static string BuildTestSystemPrompt(int count, IReadOnlyList<string> questionTypes)
    {
        var types = string.Join(", ", questionTypes);
        return $$"""
                 Generate exactly {{count}} test questions. Return ONLY valid JSON:
                 {"questions":[{"questionType":"single_choice|multi_select|fill_blank|open_answer","content":"...","fillSentence":"...or null","targetWordTerm":"...","options":[{"text":"...","isCorrect":true}]}]}
                 Use these question types: {{types}}
                 """;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to get block title suggestion from OpenAI")]
    private static partial void LogTitleSuggestionWarning(ILogger logger, Exception ex);
}
