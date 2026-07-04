using System.Text.Json;
using Lexify.Application.AI.Dtos;

namespace Lexify.Application.AI;

public static class AIResponseValidator
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static AIValidationResult Validate(string rawJson, string rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
            return AIValidationResult.Fail("AI returned an empty response.");

        // Some models keep rambling with chatty filler text after the JSON object is complete.
        // Extract just the first balanced {...} object and ignore anything trailing it.
        var jsonObject = ExtractFirstJsonObject(rawJson);
        if (jsonObject is null)
            return AIValidationResult.Fail("AI response does not contain a JSON object.");

        FormatWordsResult? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<FormatWordsResult>(jsonObject, JsonOptions);
        }
        catch (JsonException ex)
        {
            return AIValidationResult.Fail($"AI response is not valid JSON: {ex.Message}");
        }

        if (parsed is null || parsed.Words.Count == 0)
            return AIValidationResult.Fail("AI returned no words.");

        var inputLines = rawInput
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(l => l.Length > 0)
            .ToList();

        if (inputLines.Count == 0)
            return AIValidationResult.Fail("Input text is empty.");

        var recognizedTerms = parsed.Words
            .Select(w => w.Term.Trim().ToLowerInvariant())
            .ToHashSet();

        int recognized = inputLines.Count(line =>
        {
            var normalized = line.ToLowerInvariant();
            return recognizedTerms.Any(term =>
                normalized.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                term.Contains(normalized, StringComparison.OrdinalIgnoreCase));
        });

        double ratio = (double)recognized / inputLines.Count;
        if (ratio < 0.5)
            return AIValidationResult.Fail(
                $"AI recognized only {recognized}/{inputLines.Count} input lines ({ratio:P0}). Minimum is 50%.");

        return AIValidationResult.Ok(parsed);
    }

    /// <summary>Finds the first balanced top-level {...} object in text and ignores anything before/after it.</summary>
    public static string? ExtractFirstJsonObject(string text)
    {
        var start = text.IndexOf('{');
        if (start < 0) return null;

        var depth = 0;
        var inString = false;
        var escaped = false;

        for (var i = start; i < text.Length; i++)
        {
            var c = text[i];

            if (inString)
            {
                if (escaped) escaped = false;
                else if (c == '\\') escaped = true;
                else if (c == '"') inString = false;
                continue;
            }

            switch (c)
            {
                case '"': inString = true; break;
                case '{': depth++; break;
                case '}':
                    depth--;
                    if (depth == 0) return text[start..(i + 1)];
                    break;
            }
        }

        return null;
    }
}

public sealed record AIValidationResult(bool IsValid, string? ErrorMessage, FormatWordsResult? ParsedResult)
{
    public static AIValidationResult Ok(FormatWordsResult result) => new(true, null, result);
    public static AIValidationResult Fail(string error) => new(false, error, null);
}
