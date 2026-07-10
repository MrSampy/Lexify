using System.Text.Json;
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

    /// <summary>
    /// Asks the server to constrain decoding to valid JSON (grammar-level, not just a prompt
    /// instruction). Supported by most OpenAI-compatible local servers (llama.cpp, vLLM, Lemonade);
    /// servers that don't recognize the field generally just ignore it.
    /// </summary>
    [JsonPropertyName("response_format")]
    public OpenAIResponseFormat? ResponseFormat { get; init; }
}

internal sealed class OpenAIResponseFormat
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "json_object";

    /// <summary>
    /// Grammar-level schema for "type":"json_schema" requests. Must stay null (and therefore
    /// omitted — see JsonIgnore below) for plain "json_object" requests: llama.cpp-backed servers
    /// reject a literal "json_schema": null field on those.
    /// </summary>
    [JsonPropertyName("json_schema")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OpenAIJsonSchema? JsonSchema { get; init; }

    public static OpenAIResponseFormat JsonObject() => new() { Type = "json_object" };

    public static OpenAIResponseFormat ForSchema(string name, JsonElement schema, bool strict = true) =>
        new() { Type = "json_schema", JsonSchema = new OpenAIJsonSchema { Name = name, Strict = strict, Schema = schema } };
}

internal sealed class OpenAIJsonSchema
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = default!;

    [JsonPropertyName("strict")]
    public bool Strict { get; init; } = true;

    [JsonPropertyName("schema")]
    public JsonElement Schema { get; init; }
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
