namespace Lexify.Infrastructure.Settings;

public sealed class OpenAISettings
{
    public string BaseUrl { get; init; } = "https://api.openai.com";
    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = "gpt-4o-mini";
    public int TimeoutSeconds { get; init; } = 120;
}
