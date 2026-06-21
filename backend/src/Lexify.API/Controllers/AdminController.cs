using Lexify.API.Filters;
using Lexify.Application.Admin.Commands.ChangeUserRole;
using Lexify.Application.Admin.Commands.DeleteUser;
using Lexify.Application.Admin.Commands.ImpersonateUser;
using Lexify.Application.Admin.Commands.RestoreUser;
using Lexify.Application.Admin.Commands.SuspendUser;
using Lexify.Application.Admin.Commands.UpdateSystemSetting;
using Lexify.Application.Admin.Queries.GetAdminUsers;
using Lexify.Application.Admin.Queries.GetAiCallsChart;
using Lexify.Application.Admin.Queries.GetDashboardStats;
using Lexify.Application.Admin.Queries.GetRegistrationsChart;
using Lexify.Application.Admin.Queries.GetSystemSettings;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lexify.API.Controllers;

[Authorize]
[AdminOnly]
[Route("api/admin")]
public sealed class AdminController(ISender sender) : BaseApiController
{
    // --- Stats & Charts ---

    [HttpGet("stats")]
    public async Task<IActionResult> GetDashboardStats(CancellationToken ct) =>
        ToActionResult(await sender.Send(new GetDashboardStatsQuery(), ct));

    [HttpGet("charts/registrations")]
    public async Task<IActionResult> GetRegistrationsChart(
        [FromQuery] int days = 30, CancellationToken ct = default) =>
        ToActionResult(await sender.Send(new GetRegistrationsChartQuery(days), ct));

    [HttpGet("charts/ai-calls")]
    public async Task<IActionResult> GetAiCallsChart(
        [FromQuery] int hours = 24, CancellationToken ct = default) =>
        ToActionResult(await sender.Send(new GetAiCallsChartQuery(hours), ct));

    // --- User Management ---

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? role,
        [FromQuery] string? status,
        [FromQuery] string? email,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default) =>
        ToActionResult(await sender.Send(new GetAdminUsersQuery(role, status, email, page, pageSize), ct));

    [HttpPut("users/{id:guid}/suspend")]
    public async Task<IActionResult> SuspendUser(Guid id, CancellationToken ct) =>
        ToActionResult(await sender.Send(new SuspendUserCommand(id), ct));

    [HttpPut("users/{id:guid}/restore")]
    public async Task<IActionResult> RestoreUser(Guid id, CancellationToken ct) =>
        ToActionResult(await sender.Send(new RestoreUserCommand(id), ct));

    [HttpDelete("users/{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken ct) =>
        ToActionResult(await sender.Send(new DeleteUserCommand(id), ct));

    [HttpPut("users/{id:guid}/role")]
    public async Task<IActionResult> ChangeUserRole(
        Guid id, [FromBody] ChangeUserRoleRequest body, CancellationToken ct) =>
        ToActionResult(await sender.Send(new ChangeUserRoleCommand(id, body.Role), ct));

    [HttpPost("users/{id:guid}/impersonate")]
    public async Task<IActionResult> ImpersonateUser(Guid id, CancellationToken ct) =>
        ToActionResult(await sender.Send(new ImpersonateUserCommand(id), ct));

    // --- System Settings ---

    [HttpGet("settings")]
    public async Task<IActionResult> GetSystemSettings(CancellationToken ct) =>
        ToActionResult(await sender.Send(new GetSystemSettingsQuery(), ct));

    [HttpPut("settings/{key}")]
    public async Task<IActionResult> UpdateSystemSetting(
        string key, [FromBody] UpdateSystemSettingRequest body, CancellationToken ct) =>
        ToActionResult(await sender.Send(new UpdateSystemSettingCommand(key, body.Value), ct));
}

public sealed record ChangeUserRoleRequest(string Role);
public sealed record UpdateSystemSettingRequest(string Value);
