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

public sealed partial class OllamaProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<OllamaSettings> options,
    ILogger<OllamaProvider> logger)
    : IAIProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly OllamaSettings _settings = options.Value;

    public async IAsyncEnumerable<string> StreamFormatWordsAsync(
        string rawText,
        string targetLanguage,
        string nativeLanguage,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var request = new OllamaChatRequest
        {
            Model = _settings.Model,
            Messages =
            [
                new OllamaMessage { Role = "system", Content = BuildFormatSystemPrompt(nativeLanguage) },
                new OllamaMessage { Role = "user", Content = BuildFormatUserPrompt(rawText, targetLanguage, nativeLanguage) }
            ],
            Options = new OllamaOptions { Temperature = 0.1 },
            Stream = true
        };

        var http = httpClientFactory.CreateClient("ollama");
        using var response = await http.PostAsJsonAsync("/api/chat", request, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;

            OllamaStreamChunk? chunk;
            try { chunk = JsonSerializer.Deserialize<OllamaStreamChunk>(line, JsonOptions); }
            catch { continue; }

            if (chunk?.Message?.Content is { Length: > 0 } content)
                yield return content;

            if (chunk?.Done == true) break;
        }
    }

    public async Task<TestGenerationResult> GenerateTestQuestionsAsync(
        IReadOnlyList<WordDto> words,
        IReadOnlyList<string> questionTypes,
        int count,
        CancellationToken ct = default)
    {
        var wordsJson = JsonSerializer.Serialize(
            words.Select(w => new { w.Term, w.Translation, w.WordType }));

        var request = new OllamaChatRequest
        {
            Model = _settings.Model,
            Messages =
            [
                new OllamaMessage { Role = "system", Content = BuildTestSystemPrompt(count, questionTypes) },
                new OllamaMessage { Role = "user", Content = wordsJson }
            ],
            Options = new OllamaOptions { Temperature = 0.6 },
            Stream = false
        };

        var http = httpClientFactory.CreateClient("ollama");
        using var response = await http.PostAsJsonAsync("/api/chat", request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaStreamChunk>(JsonOptions, ct);
        var json = result?.Message?.Content ?? "{}";

        return JsonSerializer.Deserialize<TestGenerationResult>(json, JsonOptions)
               ?? new TestGenerationResult([]);
    }

    public async Task<string?> SuggestBlockTitleAsync(
        IReadOnlyList<string> terms,
        string language,
        CancellationToken ct = default)
    {
        var request = new OllamaChatRequest
        {
            Model = _settings.Model,
            Messages =
            [
                new OllamaMessage
                {
                    Role = "system",
                    Content = $"/no_think\nReturn ONLY a short title (2-5 words) for a {language} vocabulary block. No explanation, no punctuation."
                },
                new OllamaMessage { Role = "user", Content = string.Join(", ", terms.Take(20)) }
            ],
            Options = new OllamaOptions { Temperature = 0.3 },
            Stream = false
        };

        var http = httpClientFactory.CreateClient("ollama");
        try
        {
            using var response = await http.PostAsJsonAsync("/api/chat", request, ct);
            if (!response.IsSuccessStatusCode) return null;

            var result = await response.Content.ReadFromJsonAsync<OllamaStreamChunk>(JsonOptions, ct);
            return result?.Message?.Content?.Trim();
        }
        catch (Exception ex)
        {
            LogTitleSuggestionWarning(logger, ex);
            return null;
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            var http = httpClientFactory.CreateClient("ollama");
            using var response = await http.GetAsync("/api/tags", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static string BuildFormatSystemPrompt(string nativeLanguage) =>
        $$"""
        /no_think
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
        - wordType: word=single word, phrase=short phrase, idiom=fixed expression, expression=other
        - "translation" must ALWAYS be an actual {{nativeLanguage}} translation of the term, never an English definition, synonym, or explanation
        - If a line has no translation given, translate the term into {{nativeLanguage}} yourself
        - confidenceFlag=true if translation is uncertain (user marked with ?)
        - notes: grammar info (e.g. irregular verb, plural form)
        - Suggest a 2-4 word block title based on the content theme
        """;

    private static string BuildFormatUserPrompt(string rawText, string targetLanguage, string nativeLanguage) =>
        $"Input language: {targetLanguage}\nTranslation language: {nativeLanguage}\n\n{rawText}";

    private static string BuildTestSystemPrompt(int count, IReadOnlyList<string> questionTypes)
    {
        var types = string.Join(", ", questionTypes);
        return $$"""
                 /think
                 Generate exactly {{count}} test questions. Return ONLY valid JSON:
                 {"questions":[{"questionType":"single_choice|multi_select|fill_blank|open_answer","content":"...","fillSentence":"...or null","targetWordTerm":"...","options":[{"text":"...","isCorrect":true}]}]}
                 Use these question types: {{types}}
                 Rules: single_choice=4 options 1 correct; multi_select=4-6 options 1-3 correct; fill_blank=sentence with ___ 4 options; open_answer=empty options list
                 """;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to get block title suggestion from Ollama")]
    private static partial void LogTitleSuggestionWarning(ILogger logger, Exception ex);
}
