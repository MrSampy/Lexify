using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Lexify.Application.AI;
using Lexify.Application.AI.Dtos;
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
    public async Task<HttpResponseMessage> OpenEnrichStreamAsync(
        IReadOnlyList<ParsedImportLine> lines, string targetLanguage, string nativeLanguage, CancellationToken ct)
    {
        var request = new OpenAIChatRequest
        {
            Model = settings.Model,
            Messages =
            [
                new OpenAIMessage { Role = "system", Content = BuildEnrichSystemPrompt(targetLanguage, nativeLanguage) },
                new OpenAIMessage { Role = "user", Content = BuildEnrichUserMessage(lines) }
            ],
            Temperature = 0.2,
            Stream = true,
            MaxTokens = Math.Clamp(lines.Count * 120, 300, 3000),
            ResponseFormat = settings.SupportsJsonSchema
                ? OpenAIResponseFormat.ForSchema("enrich_words_result", AiJsonSchemas.EnrichWordsResult)
                : OpenAIResponseFormat.JsonObject()
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

    public async Task<IReadOnlyList<FillSentenceAtom>> GenerateFillSentencesAsync(
        IReadOnlyList<FillSentenceRequest> requests,
        string targetLanguage,
        string? englishLevel = null,
        CancellationToken ct = default)
    {
        var userMessage = JsonSerializer.Serialize(requests.Select(r => r.PreviousError is null
            ? (object)new { id = r.WordId.ToString(), term = r.Term, translation = r.Translation }
            : new { id = r.WordId.ToString(), term = r.Term, translation = r.Translation, previousError = r.PreviousError }));

        var request = new OpenAIChatRequest
        {
            Model = settings.Model,
            Messages =
            [
                new OpenAIMessage { Role = "system", Content = BuildFillSentenceSystemPrompt(targetLanguage, englishLevel) },
                new OpenAIMessage { Role = "user", Content = userMessage }
            ],
            Temperature = 0.7,
            Stream = false,
            MaxTokens = Math.Clamp(requests.Count * 80, 200, 2000),
            ResponseFormat = settings.SupportsJsonSchema
                ? OpenAIResponseFormat.ForSchema("fill_sentences_result", AiJsonSchemas.FillSentencesResult)
                : OpenAIResponseFormat.JsonObject()
        };

        using var response = await http.PostAsJsonAsync("/v1/chat/completions", request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OpenAIResponse>(JsonOptions, ct);
        var content = result?.Choices?[0]?.Message?.Content ?? "{}";
        var json = AIResponseValidator.ExtractFirstJsonObject(content) ?? "{}";

        var parsed = JsonSerializer.Deserialize<FillSentencesWireResult>(json, JsonOptions);
        if (parsed?.Sentences is null) return [];

        var atoms = new List<FillSentenceAtom>(parsed.Sentences.Count);
        foreach (var s in parsed.Sentences)
            if (Guid.TryParse(s.Id, out var wordId))
                atoms.Add(new FillSentenceAtom(wordId, s.Sentence));

        return atoms;
    }

    public async Task<IReadOnlyList<string>> GenerateFakeDistractorsAsync(
        string correctAnswer,
        int count,
        CancellationToken ct = default)
    {
        var request = new OpenAIChatRequest
        {
            Model = settings.Model,
            Messages =
            [
                new OpenAIMessage { Role = "system", Content = BuildFakeDistractorsSystemPrompt() },
                new OpenAIMessage { Role = "user", Content = $"Correct answer: \"{correctAnswer}\"\nGenerate {count} distractors." }
            ],
            Temperature = 0.8,
            Stream = false,
            MaxTokens = 200,
            ResponseFormat = settings.SupportsJsonSchema
                ? OpenAIResponseFormat.ForSchema("distractors_result", AiJsonSchemas.DistractorsResult)
                : OpenAIResponseFormat.JsonObject()
        };

        using var response = await http.PostAsJsonAsync("/v1/chat/completions", request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OpenAIResponse>(JsonOptions, ct);
        var content = result?.Choices?[0]?.Message?.Content ?? "{}";
        var json = AIResponseValidator.ExtractFirstJsonObject(content) ?? "{}";

        var parsed = JsonSerializer.Deserialize<DistractorsWireResult>(json, JsonOptions);
        return parsed?.Distractors ?? [];
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
                    Content = $"Return ONLY a short thematic title (2-4 words) for a {language} vocabulary block — the common topic of the words. Never enumerate or repeat the words themselves. No explanation, no numbering, no punctuation."
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

    /// <summary>
    /// Unlike the old format prompt, this one never asks the LLM to split lines or decide what the
    /// term/translation are for the majority of input — ImportLineParser already did that
    /// deterministically. The LLM's job is enrichment (word type, alternatives, notes, example
    /// sentence) and, for the minority of lines the parser couldn't split, extraction+translation.
    /// </summary>
    public static string BuildEnrichSystemPrompt(string targetLanguage, string nativeLanguage) =>
        $$"""
        Return ONLY a valid JSON object — no markdown, no explanation, no extra text or commentary before or after the JSON. The response must end immediately after the closing '}'.
        You enrich pre-parsed {{targetLanguage}} vocabulary entries with {{nativeLanguage}} translations.
        Input is a JSON array; each item has an "id" and either ("term" and "translation") or "raw".
        - For items with "term" and "translation": copy them into your output EXACTLY as given, character for character — never correct, retranslate, or rephrase them.
        - For items with "raw": the line could not be split automatically. Extract the {{targetLanguage}} term yourself and translate it into {{nativeLanguage}}.
        For EVERY item, also add:
        - wordType: "word" (single word), "phrase" (short phrase), "idiom" (fixed expression), or "expression" (anything else)
        - alternativeTranslations: other common {{nativeLanguage}} translations, as a list (empty [] if none)
        - notes: brief grammar info (irregular verb, plural form, construction pattern) or null
        - exampleSentence: one short {{targetLanguage}} sentence using the term, or null
        - confidenceNote: null unless the given translation looks wrong or ambiguous — then briefly explain why
        Output exactly one item per input item, using the SAME "id" values — never add, drop, merge, or reorder items.
        Schema:
        {
          "suggestedTitle": "a specific 2-4 word title describing the topic connecting these words, or null — never a generic label like 'Dictionary' or 'Word List'",
          "words": [
            {
              "id": 0,
              "term": "string",
              "translation": "string",
              "wordType": "word|phrase|idiom|expression",
              "alternativeTranslations": ["string"],
              "notes": "string or null",
              "exampleSentence": "string or null",
              "confidenceNote": "string or null"
            }
          ]
        }
        """;

    private static string BuildEnrichUserMessage(IReadOnlyList<ParsedImportLine> lines)
    {
        var items = lines.Select(line => line.IsParsed
            ? (object)new { id = line.Id, term = line.Term, translation = line.Translation }
            : new { id = line.Id, raw = line.RawLine });
        return JsonSerializer.Serialize(items);
    }

    /// <summary>
    /// Deliberately asks for a sentence CONTAINING the term rather than a pre-blanked one — models
    /// write more natural sentences when they don't have to simultaneously invent content and track
    /// where to place a placeholder. FillSentenceValidator/blanking (Application layer) does the
    /// blanking deterministically afterward.
    /// </summary>
    private static string BuildFillSentenceSystemPrompt(string targetLanguage, string? englishLevel)
    {
        var levelRule = englishLevel is null
            ? string.Empty
            : $" Write at CEFR {englishLevel} level — vocabulary and grammar no harder, and not much easier, than that.";
        return $$"""
            Return ONLY a valid JSON object — no markdown, no explanation, no extra text before or after the JSON.
            You write one example sentence per input word, for a {{targetLanguage}} vocabulary quiz.
            Input is a JSON array of {"id","term","translation"}, sometimes with "previousError" — if
            present, your last attempt for that word failed for that reason; fix it this time.
            For each item, write ONE natural {{targetLanguage}} sentence that:
            - uses the exact given term ONCE, in its base/dictionary form
            - is 8-16 words long
            - makes the word's meaning guessable from context
            - does NOT define, explain, or translate the word{{levelRule}}
            Output exactly one item per input item, using the SAME "id" values — never add, drop, merge, or reorder items.
            Schema: {"sentences":[{"id":"string","sentence":"string"}]}
            """;
    }

    private static string BuildFakeDistractorsSystemPrompt() =>
        """
        Return ONLY a valid JSON object — no markdown, no explanation, no extra text before or after the JSON.
        Generate plausible but WRONG quiz answers, in the SAME language as the given correct answer — same
        part of speech and register, clearly incorrect, and NOT a synonym or near-synonym of the correct answer.
        Schema: {"distractors":["string"]}
        """;

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to get block title suggestion from {ProviderName}")]
    private static partial void LogTitleSuggestionWarning(ILogger logger, Exception ex, string providerName);
}
