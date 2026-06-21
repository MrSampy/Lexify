using System.Text.Json.Serialization;

namespace Lexify.Infrastructure.AI.Models;

internal sealed class OllamaChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; init; } = default!;

    [JsonPropertyName("messages")]
    public IReadOnlyList<OllamaMessage> Messages { get; init; } = [];

    [JsonPropertyName("options")]
    public OllamaOptions? Options { get; init; }

    [JsonPropertyName("stream")]
    public bool Stream { get; init; } = true;
}

internal sealed class OllamaMessage
{
    [JsonPropertyName("role")]
    public string Role { get; init; } = default!;

    [JsonPropertyName("content")]
    public string Content { get; init; } = default!;
}

internal sealed class OllamaOptions
{
    [JsonPropertyName("temperature")]
    public double Temperature { get; init; }

    [JsonPropertyName("num_ctx")]
    public int NumCtx { get; init; } = 8192;
}

internal sealed class OllamaStreamChunk
{
    [JsonPropertyName("message")]
    public OllamaMessage? Message { get; init; }

    [JsonPropertyName("done")]
    public bool Done { get; init; }
}
