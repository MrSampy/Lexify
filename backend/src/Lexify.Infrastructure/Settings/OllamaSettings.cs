namespace Lexify.Infrastructure.Settings;

public sealed class OllamaSettings
{
    public string BaseUrl { get; init; } = "http://localhost:11434";
    public string Model { get; init; } = "qwen3:8b";
    public int TimeoutSeconds { get; init; } = 120;
}
