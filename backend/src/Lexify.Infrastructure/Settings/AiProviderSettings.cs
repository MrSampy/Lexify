namespace Lexify.Infrastructure.Settings;

public sealed class AiProviderSettings
{
    public string Name { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 120;

    /// <summary>
    /// Whether this provider's server enforces response_format.json_schema as a decoding-time
    /// grammar constraint. Set to false for providers known to reject or mishandle the field —
    /// requests then fall back to plain "json_object" mode.
    /// </summary>
    public bool SupportsJsonSchema { get; init; } = true;
}
