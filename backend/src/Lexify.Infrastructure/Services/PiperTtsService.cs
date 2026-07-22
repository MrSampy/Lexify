using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Lexify.Application.Abstractions;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using Lexify.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexify.Infrastructure.Services;

public sealed partial class PiperTtsService(
    IHttpClientFactory httpClientFactory,
    IOptions<PiperSettings> options,
    ISystemSettingRepository settingRepository,
    IWordRepository wordRepository,
    IWordBlockRepository blockRepository,
    ILanguageRepository languageRepository,
    ILogger<PiperTtsService> logger)
    : ITtsService
{
    private const string WavContentType = "audio/wav";
    /// <summary>Upper bound on free-form synthesis text (a chat reply), to bound Piper latency and cache size.</summary>
    private const int MaxTextLength = 600;
    private readonly PiperSettings _settings = options.Value;

    public async Task<TtsCapabilities> GetCapabilitiesAsync(CancellationToken ct = default)
    {
        var enabled = await IsEnabledAsync(ct);
        // Only advertise languages that actually have a configured voice — the client uses this to
        // decide, per language, whether to request server audio or go straight to browser TTS.
        var languages = enabled ? _settings.Voices.Keys.ToList() : [];
        return new TtsCapabilities(enabled, languages);
    }

    public async Task<TtsAudio?> SynthesizeWordAsync(Guid wordId, Guid userId, CancellationToken ct = default)
    {
        if (!await IsEnabledAsync(ct))
            return null;

        var word = await wordRepository.GetByIdAsync(wordId, ct);
        if (word is null)
            return null;

        // Ownership: the word must belong to a block owned by the caller.
        var block = await blockRepository.GetByIdAsync(word.BlockId, ct);
        if (block is null || block.UserId != userId)
            return null;

        var language = await languageRepository.GetByIdAsync(block.LanguageId, ct);
        if (language is null || !_settings.Voices.TryGetValue(language.Code, out var voice) ||
            string.IsNullOrWhiteSpace(voice))
            return null; // no server voice for this language → client falls back to browser TTS

        var term = word.Term.Trim();
        if (term.Length == 0)
            return null;

        // Cache key is keyed by voice + text (not word id), so editing a term naturally yields a new
        // file and the stale one is simply orphaned (a cleanup job can reap old files later).
        var cached = await TryReadCacheAsync(voice, term, ct);
        if (cached is not null)
            return new TtsAudio(cached, WavContentType);

        var audio = await SynthesizeViaPiperAsync(term, voice, ct);
        if (audio is null)
            return null;

        await WriteCacheAsync(voice, term, audio, ct);
        return new TtsAudio(audio, WavContentType);
    }

    public async Task<TtsAudio?> SynthesizeTextAsync(string text, string languageCode, CancellationToken ct = default)
    {
        if (!await IsEnabledAsync(ct))
            return null;

        var trimmed = text?.Trim() ?? string.Empty;
        if (trimmed.Length == 0 || trimmed.Length > MaxTextLength)
            return null;

        if (!_settings.Voices.TryGetValue(languageCode, out var voice) || string.IsNullOrWhiteSpace(voice))
            return null; // no server voice for this language → client falls back to browser TTS

        var cached = await TryReadCacheAsync(voice, trimmed, ct);
        if (cached is not null)
            return new TtsAudio(cached, WavContentType);

        var audio = await SynthesizeViaPiperAsync(trimmed, voice, ct);
        if (audio is null)
            return null;

        await WriteCacheAsync(voice, trimmed, audio, ct);
        return new TtsAudio(audio, WavContentType);
    }

    private async Task<bool> IsEnabledAsync(CancellationToken ct)
    {
        if (!_settings.Enabled)
            return false;

        // Admin-editable runtime toggle. Missing row defaults to enabled (deploy flag already gates it).
        var setting = await settingRepository.GetByKeyAsync(SystemSetting.Keys.TtsEnabled, ct);
        return setting is null || !bool.TryParse(setting.Value, out var on) || on;
    }

    private async Task<byte[]?> SynthesizeViaPiperAsync(string text, string voice, CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient("piper");
            // piper-tts 1.4.2 HTTP server synthesizes at POST / with a JSON body and returns WAV bytes
            // (it mislabels the response as text/html, but the body is a valid RIFF/WAVE stream).
            using var response = await client.PostAsJsonAsync(
                "/", new { text, voice }, ct);

            if (!response.IsSuccessStatusCode)
            {
                LogPiperFailure(logger, voice, (int)response.StatusCode, null);
                return null;
            }

            return await response.Content.ReadAsByteArrayAsync(ct);
        }
        catch (Exception ex)
        {
            // Never throw: a TTS outage degrades to browser speech on the client.
            LogPiperFailure(logger, voice, 0, ex);
            return null;
        }
    }

    private string CachePath(string voice, string text)
    {
        var hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes($"{voice}|{text}")));
        return Path.Combine(_settings.CacheDir, $"{hash}.wav");
    }

    private async Task<byte[]?> TryReadCacheAsync(string voice, string text, CancellationToken ct)
    {
        try
        {
            var path = CachePath(voice, text);
            return File.Exists(path) ? await File.ReadAllBytesAsync(path, ct) : null;
        }
        catch
        {
            return null; // a cache read error just means a miss
        }
    }

    private async Task WriteCacheAsync(string voice, string text, byte[] audio, CancellationToken ct)
    {
        try
        {
            Directory.CreateDirectory(_settings.CacheDir);
            var path = CachePath(voice, text);
            // Write to a unique temp file then move, so a concurrent reader never sees a partial file.
            var tmp = $"{path}.{Guid.NewGuid():N}.tmp";
            await File.WriteAllBytesAsync(tmp, audio, ct);
            File.Move(tmp, path, overwrite: true);
        }
        catch (Exception ex)
        {
            LogCacheWriteFailure(logger, ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Piper synthesis failed for voice {Voice} (status {Status}).")]
    static partial void LogPiperFailure(ILogger logger, string voice, int status, Exception? ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to write TTS audio to disk cache.")]
    static partial void LogCacheWriteFailure(ILogger logger, Exception ex);
}
