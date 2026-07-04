namespace Lexify.Infrastructure.Settings;

public sealed class AiProviderSettings
{
    public string Name { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 120;
}
