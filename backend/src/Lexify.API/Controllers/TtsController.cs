using Lexify.Application.Abstractions;
using Lexify.API.RateLimit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Lexify.API.Controllers;

[Authorize]
[Route("api/tts")]
public sealed class TtsController(ITtsService ttsService, ICurrentUserService currentUser) : BaseApiController
{
    /// <summary>
    /// Whether server TTS is on and which language codes have a voice — the client uses this to
    /// decide, per language, between server audio and browser speech synthesis.
    /// </summary>
    [HttpGet("capabilities")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCapabilities(CancellationToken ct) =>
        Ok(await ttsService.GetCapabilitiesAsync(ct));

    /// <summary>
    /// Neural audio (WAV) for a word's term. Returns 404 when TTS is off, the language has no voice,
    /// or the word isn't the caller's — all of which the client treats as "fall back to browser TTS".
    /// </summary>
    [HttpGet("word/{wordId:guid}")]
    [EnableRateLimiting(TtsRateLimiterPolicy.PolicyName)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWordAudio(Guid wordId, CancellationToken ct)
    {
        var audio = await ttsService.SynthesizeWordAsync(wordId, currentUser.UserId, ct);
        if (audio is null)
            return NotFound();

        Response.Headers.CacheControl = "private, max-age=86400";
        return File(audio.Bytes, audio.ContentType);
    }
}
