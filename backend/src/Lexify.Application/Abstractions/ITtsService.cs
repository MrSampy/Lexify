namespace Lexify.Application.Abstractions;

/// <summary>Synthesized audio for a word: raw bytes plus the MIME type to return to the client.</summary>
public sealed record TtsAudio(byte[] Bytes, string ContentType);

/// <summary>
/// What the client needs to decide whether to use server audio or fall back to browser speech:
/// whether the feature is on, and which language codes actually have a server voice configured.
/// </summary>
public sealed record TtsCapabilities(bool Enabled, IReadOnlyList<string> Languages);

/// <summary>
/// Server-side neural text-to-speech (Piper). Synthesizes a word's term into audio, caching results.
/// Every "not available" condition (feature off, unsupported language, upstream error) returns
/// <c>null</c> rather than throwing, so the caller/controller can signal the client to fall back to
/// browser speech synthesis.
/// </summary>
public interface ITtsService
{
    Task<TtsAudio?> SynthesizeWordAsync(Guid wordId, Guid userId, CancellationToken ct = default);

    Task<TtsCapabilities> GetCapabilitiesAsync(CancellationToken ct = default);
}
