using Lexify.Application.Abstractions;
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
    /// <summary>Returns aggregate statistics for the current user.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserStats(CancellationToken ct) =>
        ToActionResult(await sender.Send(new GetUserStatsQuery(currentUser.UserId), ct));
}
