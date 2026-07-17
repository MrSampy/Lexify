namespace Lexify.Infrastructure.Settings;

public sealed class PiperSettings
{
    /// <summary>Deploy-time master switch. The admin-editable features.tts_enabled setting layers on top.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Base URL of the Piper HTTP server (the sidecar container), e.g. http://piper:5000.</summary>
    public string BaseUrl { get; init; } = "http://piper:5000";

    public int TimeoutSeconds { get; init; } = 30;

    /// <summary>Directory (a mounted volume in prod) where synthesized audio is cached to disk.</summary>
    public string CacheDir { get; init; } = "tts-cache";

    /// <summary>Maps a 2-letter language code (Language.Code) to a Piper voice model name.
    /// A language absent here has no server voice and falls back to browser TTS on the client.</summary>
    public Dictionary<string, string> Voices { get; init; } = new();
}
