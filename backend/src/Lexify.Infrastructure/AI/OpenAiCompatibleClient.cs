using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Lexify.Application.AI;
using Lexify.Application.AI.Dtos;
using Lexify.Application.Words.Dtos;
using Lexify.Infrastructure.AI.Models;
using Lexify.Infrastructure.Settings;
using Microsoft.Extensions.Logging;

namespace Lexify.Infrastructure.AI;

/// <summary>
/// Talks to any OpenAI-compatible chat completions endpoint (/v1/chat/completions) — used for every
/// entry in the "AiProviders" config list (Ollama, Lemonade, OpenAI, etc. all implement this contract).
/// </summary>
public sealed partial class OpenAiCompatibleClient(HttpClient http, AiProviderSettings settings, ILogger logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Opens the streaming request and validates the response headers/status. Throws on connection
    /// failure or a non-success status — callers use this to decide whether to fall back to the next
    /// configured provider. Once this returns successfully, the caller has committed to this provider.
    /// </summary>
    public async Task<HttpResponseMessage> OpenFormatStreamAsync(
        string rawText, string targetLanguage, string nativeLanguage, CancellationToken ct)
    {
        var lineCount = rawText.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;

        var request = new OpenAIChatRequest
        {
            Model = settings.Model,
            Messages =
            [
                new OpenAIMessage { Role = "system", Content = BuildFormatSystemPrompt(nativeLanguage) },
                new OpenAIMessage { Role = "user", Content = BuildFormatUserPrompt(rawText, targetLanguage, nativeLanguage) }
            ],
            Temperature = 0.1,
            Stream = true,
            MaxTokens = Math.Clamp(lineCount * 120, 300, 3000)
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("Accept", "text/event-stream");

        var response = await http.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        return response;
    }

    public static async IAsyncEnumerable<string> EnumerateFormatStreamAsync(
        HttpResponseMessage response, [EnumeratorCancellation] CancellationToken ct)
    {
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
        string? englishLevel = null,
        CancellationToken ct = default)
    {
        var wordsJson = JsonSerializer.Serialize(words.Select(w => new { w.Term, w.Translation, w.WordType }));
        var request = new OpenAIChatRequest
        {
            Model = settings.Model,
            Messages =
            [
                new OpenAIMessage { Role = "system", Content = BuildTestSystemPrompt(count, questionTypes, englishLevel) },
                new OpenAIMessage { Role = "user", Content = wordsJson }
            ],
            Temperature = 0.6,
            Stream = false,
            MaxTokens = Math.Clamp(count * 220, 400, 6000)
        };

        using var response = await http.PostAsJsonAsync("/v1/chat/completions", request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OpenAIResponse>(JsonOptions, ct);
        var content = result?.Choices?[0]?.Message?.Content ?? "{}";
        var json = AIResponseValidator.ExtractFirstJsonObject(content) ?? "{}";

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
            Model = settings.Model,
            Messages =
            [
                new OpenAIMessage
                {
                    Role = "system",
                    Content = $"/no_think\nReturn ONLY a short thematic title (2-4 words) for a {language} vocabulary block — the common topic of the words. Never enumerate or repeat the words themselves. No explanation, no numbering, no punctuation."
                },
                new OpenAIMessage { Role = "user", Content = string.Join(", ", terms.Take(20)) }
            ],
            Temperature = 0.3,
            Stream = false,
            MaxTokens = 20
        };

        try
        {
            using var response = await http.PostAsJsonAsync("/v1/chat/completions", request, ct);
            if (!response.IsSuccessStatusCode) return null;

            var result = await response.Content.ReadFromJsonAsync<OpenAIResponse>(JsonOptions, ct);
            return SanitizeTitle(result?.Choices?[0]?.Message?.Content);
        }
        catch (Exception ex)
        {
            LogTitleSuggestionWarning(logger, ex, settings.Name);
            return null;
        }
    }

    /// <summary>
    /// Local models sometimes ignore the "2-4 words" instruction and return an enumeration of the
    /// input words instead. Reject anything that doesn't look like a short thematic title so the
    /// caller can fall back to another source.
    /// </summary>
    private static string? SanitizeTitle(string? raw)
    {
        var title = raw?.Trim().Trim('"', '\'', '.');
        if (string.IsNullOrWhiteSpace(title)) return null;
        if (title.Any(char.IsDigit)) return null;
        if (title.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length > 5) return null;
        return title;
    }

    public static string BuildFormatSystemPrompt(string nativeLanguage) =>
        $$"""
        /no_think
        Return ONLY a valid JSON object — no markdown, no explanation, no extra text or commentary before or after the JSON. The response must end immediately after the closing '}'.
        Schema:
        {
          "suggestedTitle": "string or null",
          "words": [
            {
              "term": "string",
              "translation": "string",
              "alternativeTranslations": ["string"],
              "wordType": "word|phrase|idiom|expression",
              "notes": "string or null",
              "exampleSentence": "string or null",
              "confidenceFlag": false,
              "confidenceNote": "string or null"
            }
          ]
        }
        Rules:
        - Parse each non-empty line as one entry — never split one line into several
        - term = everything before the last " - " on the line, even if it contains "+" or grammar words like "with/without to" (move that construction detail into notes instead)
        - The text after the last " - " is the user's translation ONLY if it is written in {{nativeLanguage}}; if it is in English (a definition or synonym) or missing, IGNORE it and translate the term into {{nativeLanguage}} yourself
        - "translation" holds exactly ONE {{nativeLanguage}} word or short phrase — if the user listed several variants separated by commas or slashes, put the first in "translation" and the rest in "alternativeTranslations" (empty list [] when there are no extras)
        - wordType: word=single word, phrase=short phrase, idiom=fixed expression, expression=other
        - confidenceFlag=true if translation is uncertain (user marked with ?)
        - notes: grammar info (e.g. irregular verb, plural form, construction pattern)
        - "suggestedTitle": a specific 2-4 word title describing what actually connects these words (a topic, source, or theme) — never a generic label like "Dictionary", "Word List", or "Vocabulary"
        """;

    private static string BuildFormatUserPrompt(string rawText, string targetLanguage, string nativeLanguage) =>
        $"Input language: {targetLanguage}\nTranslation language: {nativeLanguage}\n\n{rawText}";

    public static string BuildTestSystemPrompt(int count, IReadOnlyList<string> questionTypes, string? englishLevel = null)
    {
        var types = string.Join(", ", questionTypes);
        var levelRule = englishLevel is null
            ? string.Empty
            : $"\nThe learner's English level is CEFR {englishLevel}: write question sentences, fill_blank sentences, and distractor options using vocabulary and grammar appropriate for {englishLevel} — not harder, not much easier.";
        return $$"""
                 /no_think
                 Generate exactly {{count}} test questions from the user's word list. Return ONLY a valid JSON object — no markdown, no explanation, no extra text before or after the JSON.{{levelRule}}
                 Schema:
                 {"questions":[{"questionType":"single_choice|multi_select|fill_blank|open_answer","content":"...","fillSentence":"...or null","targetWordTerm":"...","options":[{"text":"...","isCorrect":true}]}]}
                 Use ONLY these question types: {{types}}
                 Strict rules per questionType:
                 - single_choice: exactly 4 options, exactly 1 with "isCorrect":true; "fillSentence" is null
                 - multi_select: 4-6 options, 1-3 with "isCorrect":true; "fillSentence" is null
                 - fill_blank: "fillSentence" is a full sentence containing exactly ONE blank written as ___ ; exactly 4 options, exactly 1 correct; the correct option fits the blank
                 - open_answer: "options" is an empty list []; "fillSentence" is null
                 - "targetWordTerm" is always the exact term from the input word list that the question tests
                 Examples (one question per type — follow this format exactly):
                 {"questionType":"single_choice","content":"What is the translation of 'perro'?","fillSentence":null,"targetWordTerm":"perro","options":[{"text":"dog","isCorrect":true},{"text":"cat","isCorrect":false},{"text":"bird","isCorrect":false},{"text":"horse","isCorrect":false}]}
                 {"questionType":"multi_select","content":"Select all words that are animals.","fillSentence":null,"targetWordTerm":"perro","options":[{"text":"perro","isCorrect":true},{"text":"gato","isCorrect":true},{"text":"mesa","isCorrect":false},{"text":"silla","isCorrect":false},{"text":"libro","isCorrect":false}]}
                 {"questionType":"fill_blank","content":"Complete the sentence with the correct word.","fillSentence":"El ___ ladra en el jardín.","targetWordTerm":"perro","options":[{"text":"perro","isCorrect":true},{"text":"gato","isCorrect":false},{"text":"sol","isCorrect":false},{"text":"libro","isCorrect":false}]}
                 {"questionType":"open_answer","content":"Translate 'perro' into English.","fillSentence":null,"targetWordTerm":"perro","options":[]}
                 """;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to get block title suggestion from {ProviderName}")]
    private static partial void LogTitleSuggestionWarning(ILogger logger, Exception ex, string providerName);
}
