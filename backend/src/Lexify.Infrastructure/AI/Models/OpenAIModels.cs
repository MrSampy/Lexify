using System.Text.Json.Serialization;

namespace Lexify.Infrastructure.AI.Models;

internal sealed class OpenAIChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; init; } = default!;

    [JsonPropertyName("messages")]
    public IReadOnlyList<OpenAIMessage> Messages { get; init; } = [];

    [JsonPropertyName("temperature")]
    public double Temperature { get; init; }

    [JsonPropertyName("stream")]
    public bool Stream { get; init; }

    /// <summary>
    /// Hard cap on generated tokens. Some local models (notably smaller/quantized Llama builds) don't
    /// reliably stop at the end of the requested JSON and keep rambling on with chatty filler text —
    /// this bounds the damage and the cost in latency.
    /// </summary>
    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; init; }
}

internal sealed class OpenAIMessage
{
    [JsonPropertyName("role")]
    public string Role { get; init; } = default!;

    [JsonPropertyName("content")]
    public string Content { get; init; } = default!;
}

internal sealed class OpenAIStreamChunk
{
    [JsonPropertyName("choices")]
    public IReadOnlyList<OpenAIStreamChoice>? Choices { get; init; }
}

internal sealed class OpenAIStreamChoice
{
    [JsonPropertyName("delta")]
    public OpenAIDelta? Delta { get; init; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; init; }
}

internal sealed class OpenAIDelta
{
    [JsonPropertyName("content")]
    public string? Content { get; init; }
}

internal sealed class OpenAIResponse
{
    [JsonPropertyName("choices")]
    public IReadOnlyList<OpenAIChoice>? Choices { get; init; }
}

internal sealed class OpenAIChoice
{
    [JsonPropertyName("message")]
    public OpenAIMessage? Message { get; init; }
}
