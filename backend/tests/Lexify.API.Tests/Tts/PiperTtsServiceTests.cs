using System.Security.Cryptography;
using System.Text;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using Lexify.Infrastructure.Services;
using Lexify.Infrastructure.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Lexify.API.Tests.Tts;

/// <summary>
/// Pure unit tests for the TTS service's decision/cache logic — no Docker, no real Piper. Every
/// "not available" path must return null (so the client falls back to browser speech), and a cache
/// hit must never touch the HTTP client.
/// </summary>
public class PiperTtsServiceTests : IDisposable
{
    private readonly IHttpClientFactory _http = Substitute.For<IHttpClientFactory>();
    private readonly ISystemSettingRepository _settings = Substitute.For<ISystemSettingRepository>();
    private readonly IWordRepository _words = Substitute.For<IWordRepository>();
    private readonly IWordBlockRepository _blocks = Substitute.For<IWordBlockRepository>();
    private readonly ILanguageRepository _langs = Substitute.For<ILanguageRepository>();
    private readonly string _cacheDir =
        Path.Combine(Path.GetTempPath(), "lexify-tts-tests-" + Guid.NewGuid().ToString("N"));

    private PiperTtsService Create(bool deployEnabled = true, Dictionary<string, string>? voices = null) =>
        new(
            _http,
            Options.Create(new PiperSettings
            {
                Enabled = deployEnabled,
                BaseUrl = "http://piper:5000",
                CacheDir = _cacheDir,
                Voices = voices ?? new Dictionary<string, string> { ["en"] = "en_US-amy-medium" },
            }),
            _settings, _words, _blocks, _langs,
            NullLogger<PiperTtsService>.Instance);

    [Fact]
    public async Task DeployFlagOff_ReturnsNull_AndNeverCallsPiper()
    {
        var result = await Create(deployEnabled: false).SynthesizeWordAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Null(result);
        _http.DidNotReceive().CreateClient(Arg.Any<string>());
    }

    [Fact]
    public async Task RuntimeToggleOff_ReturnsNull()
    {
        _settings.GetByKeyAsync(SystemSetting.Keys.TtsEnabled, Arg.Any<CancellationToken>())
            .Returns(new SystemSetting(SystemSetting.Keys.TtsEnabled, "false", "bool"));

        var result = await Create().SynthesizeWordAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Null(result);
        _http.DidNotReceive().CreateClient(Arg.Any<string>());
    }

    [Fact]
    public async Task ForeignWord_ReturnsNull()
    {
        var owner = Guid.NewGuid();
        var word = new Word(Guid.NewGuid(), "apple", "яблуко");
        var block = new WordBlock(Guid.NewGuid(), 1, "Someone else's"); // different owner
        _words.GetByIdAsync(word.Id, Arg.Any<CancellationToken>()).Returns(word);
        _blocks.GetByIdAsync(word.BlockId, Arg.Any<CancellationToken>()).Returns(block);

        var result = await Create().SynthesizeWordAsync(word.Id, owner);

        Assert.Null(result);
        _http.DidNotReceive().CreateClient(Arg.Any<string>());
    }

    [Fact]
    public async Task LanguageWithoutVoice_ReturnsNull()
    {
        var owner = Guid.NewGuid();
        var word = new Word(Guid.NewGuid(), "cześć", "hi");
        var block = new WordBlock(owner, 6, "Polish");
        _words.GetByIdAsync(word.Id, Arg.Any<CancellationToken>()).Returns(word);
        _blocks.GetByIdAsync(word.BlockId, Arg.Any<CancellationToken>()).Returns(block);
        _langs.GetByIdAsync(6, Arg.Any<CancellationToken>())
            .Returns(new Language("pl", "Polish", "Polski", true, 6));

        // Voice map only has "en" → Polish has no server voice.
        var result = await Create().SynthesizeWordAsync(word.Id, owner);

        Assert.Null(result);
        _http.DidNotReceive().CreateClient(Arg.Any<string>());
    }

    [Fact]
    public async Task CacheHit_ReturnsCachedBytes_WithoutCallingPiper()
    {
        var owner = Guid.NewGuid();
        var word = new Word(Guid.NewGuid(), "apple", "яблуко");
        var block = new WordBlock(owner, 1, "Fruits");
        _words.GetByIdAsync(word.Id, Arg.Any<CancellationToken>()).Returns(word);
        _blocks.GetByIdAsync(word.BlockId, Arg.Any<CancellationToken>()).Returns(block);
        _langs.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new Language("en", "English", "English", true, 1));

        // Pre-seed the disk cache at the exact key the service computes: sha256("{voice}|{term}").
        var expected = new byte[] { 1, 2, 3, 4, 5 };
        Directory.CreateDirectory(_cacheDir);
        var hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes("en_US-amy-medium|apple")));
        await File.WriteAllBytesAsync(Path.Combine(_cacheDir, $"{hash}.wav"), expected);

        var result = await Create().SynthesizeWordAsync(word.Id, owner);

        Assert.NotNull(result);
        Assert.Equal(expected, result!.Bytes);
        Assert.Equal("audio/wav", result.ContentType);
        _http.DidNotReceive().CreateClient(Arg.Any<string>());
    }

    [Fact]
    public async Task Capabilities_WhenEnabled_ListsConfiguredVoiceLanguages()
    {
        var caps = await Create(voices: new Dictionary<string, string>
        {
            ["en"] = "en_US-amy-medium",
            ["uk"] = "uk_UA-ukrainian_tts-medium",
        }).GetCapabilitiesAsync();

        Assert.True(caps.Enabled);
        Assert.Contains("en", caps.Languages);
        Assert.Contains("uk", caps.Languages);
    }

    [Fact]
    public async Task Capabilities_WhenDeployFlagOff_IsDisabledWithNoLanguages()
    {
        var caps = await Create(deployEnabled: false).GetCapabilitiesAsync();

        Assert.False(caps.Enabled);
        Assert.Empty(caps.Languages);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_cacheDir)) Directory.Delete(_cacheDir, recursive: true); }
        catch { /* best-effort temp cleanup */ }
        GC.SuppressFinalize(this);
    }
}
