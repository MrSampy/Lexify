using Lexify.API.Filters;
using Lexify.Application.Admin.Queries.GetAiCallsChart;
using Lexify.Application.Admin.Queries.GetDashboardStats;
using Lexify.Application.Admin.Queries.GetRegistrationsChart;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lexify.API.Controllers;

[Authorize]
[AdminOnly]
[Route("api/admin")]
public sealed class AdminController(ISender sender) : BaseApiController
{
    [HttpGet("stats")]
    public async Task<IActionResult> GetDashboardStats(CancellationToken ct) =>
        ToActionResult(await sender.Send(new GetDashboardStatsQuery(), ct));

    [HttpGet("charts/registrations")]
    public async Task<IActionResult> GetRegistrationsChart([FromQuery] int days = 30, CancellationToken ct = default) =>
        ToActionResult(await sender.Send(new GetRegistrationsChartQuery(days), ct));

    [HttpGet("charts/ai-calls")]
    public async Task<IActionResult> GetAiCallsChart([FromQuery] int hours = 24, CancellationToken ct = default) =>
        ToActionResult(await sender.Send(new GetAiCallsChartQuery(hours), ct));
}
