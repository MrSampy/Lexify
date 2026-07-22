using Lexify.Application.Abstractions;
using Lexify.Application.Stats.Queries.GetAccuracy;
using Lexify.Application.Stats.Queries.GetActivity;
using Lexify.Application.Stats.Queries.GetConversationStats;
using Lexify.Application.Stats.Queries.GetForecast;
using Lexify.Application.Stats.Queries.GetMastery;
using Lexify.Application.Stats.Queries.GetProblemWords;
using Lexify.Application.Stats.Queries.GetUserStats;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lexify.API.Controllers;

[Authorize]
[Route("api/stats")]
public sealed class StatsController(ISender sender, ICurrentUserService currentUser) : BaseApiController
{
    /// <summary>Returns aggregate statistics for the current user (counts, due words, current streak).</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserStats(CancellationToken ct) =>
        ToActionResult(await sender.Send(new GetUserStatsQuery(currentUser.UserId), ct));

    /// <summary>Daily review counts over the last <paramref name="days"/> days, plus current/longest streaks.</summary>
    [HttpGet("activity")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActivity([FromQuery] int days = 90, CancellationToken ct = default) =>
        ToActionResult(await sender.Send(new GetActivityQuery(currentUser.UserId, days), ct));

    /// <summary>Distribution of the user's words across SM-2 mastery buckets.</summary>
    [HttpGet("mastery")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMastery(CancellationToken ct) =>
        ToActionResult(await sender.Send(new GetMasteryQuery(currentUser.UserId), ct));

    /// <summary>Scheduled review load per day for the next <paramref name="days"/> days (day 0 includes overdue).</summary>
    [HttpGet("forecast")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForecast([FromQuery] int days = 14, CancellationToken ct = default) =>
        ToActionResult(await sender.Send(new GetForecastQuery(currentUser.UserId, days), ct));

    /// <summary>Words the user keeps forgetting: leeches and confidence-flagged words, worst first.</summary>
    [HttpGet("problem-words")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProblemWords([FromQuery] int limit = 20, CancellationToken ct = default) =>
        ToActionResult(await sender.Send(new GetProblemWordsQuery(currentUser.UserId, limit), ct));

    /// <summary>"Talk to Lexi" practice totals: ended sessions, distinct words practised, average stars.</summary>
    [HttpGet("conversations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConversationStats(CancellationToken ct) =>
        ToActionResult(await sender.Send(new GetConversationStatsQuery(currentUser.UserId), ct));

    /// <summary>Per-day recall accuracy over the last <paramref name="days"/> days.</summary>
    [HttpGet("accuracy")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAccuracy([FromQuery] int days = 30, CancellationToken ct = default) =>
        ToActionResult(await sender.Send(new GetAccuracyQuery(currentUser.UserId, days), ct));
}
