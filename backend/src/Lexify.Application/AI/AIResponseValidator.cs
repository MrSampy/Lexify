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

        FormatWordsResult? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<FormatWordsResult>(rawJson, JsonOptions);
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
}

public sealed record AIValidationResult(bool IsValid, string? ErrorMessage, FormatWordsResult? ParsedResult)
{
    public static AIValidationResult Ok(FormatWordsResult result) => new(true, null, result);
    public static AIValidationResult Fail(string error) => new(false, error, null);
}
