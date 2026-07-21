namespace Lexify.Domain.Entities;

public sealed class AiCallLog
{
    public Guid Id { get; private set; }
    public Guid? UserId { get; private set; }
    public string CallType { get; private set; } = default!;
    public string Provider { get; private set; } = default!;
    public string Model { get; private set; } = default!;
    public int? InputTokens { get; private set; }
    public int? OutputTokens { get; private set; }
    public int DurationMs { get; private set; }
    public bool Success { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? InputHash { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private AiCallLog() { }

    public AiCallLog(Guid? userId, string callType, string provider, string model,
        int durationMs, bool success, string? errorMessage = null,
        int? inputTokens = null, int? outputTokens = null, string? inputHash = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        CallType = callType;
        Provider = provider;
        Model = model;
        DurationMs = durationMs;
        Success = success;
        ErrorMessage = errorMessage;
        InputTokens = inputTokens;
        OutputTokens = outputTokens;
        InputHash = inputHash;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static class CallTypes
    {
        public const string FormatWords = "format_words";
        public const string GenerateFillSentences = "generate_fill_sentences";
        public const string GenerateDistractors = "generate_distractors";
        public const string GenerateDefinitions = "generate_definitions";
        public const string SuggestTitle = "suggest_title";
    }
}
