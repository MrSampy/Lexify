using Lexify.API.Filters;
using Lexify.Application.Admin.Commands.AddLanguage;
using Lexify.Application.Admin.Commands.ChangeUserRole;
using Lexify.Application.Admin.Commands.DeleteUser;
using Lexify.Application.Admin.Commands.ImpersonateUser;
using Lexify.Application.Admin.Commands.RestoreUser;
using Lexify.Application.Admin.Commands.SuspendUser;
using Lexify.Application.Admin.Commands.ToggleLanguage;
using Lexify.Application.Admin.Commands.UpdateSystemSetting;
using Lexify.Application.Admin.Queries.GetAdminUsers;
using Lexify.Application.Admin.Queries.GetAiCallsChart;
using Lexify.Application.Admin.Queries.GetAiLogs;
using Lexify.Application.Admin.Queries.GetAuditLogs;
using Lexify.Application.Admin.Queries.GetAiStats;
using Lexify.Application.Admin.Queries.GetAiStatus;
using Lexify.Application.Admin.Queries.GetDashboardStats;
using Lexify.Application.Admin.Queries.GetLanguages;
using Lexify.Application.Admin.Queries.GetRegistrationsChart;
using Lexify.Application.Admin.Queries.GetSystemSettings;
using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

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

    /// <summary>
    /// Operational snapshot for the admin dashboard: health-check results, Hangfire failed-job
    /// count, and the age of the newest database backup (when the backup volume is mounted).
    /// </summary>
    [HttpGet("system-health")]
    public async Task<IActionResult> GetSystemHealth(
        [FromServices] HealthCheckService healthChecks,
        [FromServices] IConfiguration configuration,
        CancellationToken ct)
    {
        var report = await healthChecks.CheckHealthAsync(ct);
        var checks = report.Entries
            .Select(e => new { Name = e.Key, Status = e.Value.Status.ToString() })
            .ToList();

        long? failedJobs = null;
        try
        {
            failedJobs = JobStorage.Current.GetMonitoringApi().FailedCount();
        }
        catch
        {
            // Hangfire storage unavailable — surfaced as null rather than failing the endpoint.
        }

        DateTimeOffset? lastBackupAt = null;
        var backupsPath = configuration["Backups:Path"];
        if (!string.IsNullOrEmpty(backupsPath) && Directory.Exists(backupsPath))
        {
            lastBackupAt = Directory
                .EnumerateFiles(backupsPath, "*", SearchOption.AllDirectories)
                .Select(f => (DateTimeOffset?)System.IO.File.GetLastWriteTimeUtc(f))
                .DefaultIfEmpty(null)
                .Max();
        }

        return Ok(new
        {
            Status = report.Status.ToString(),
            Checks = checks,
            FailedJobs = failedJobs,
            LastBackupAt = lastBackupAt,
            BackupMonitored = !string.IsNullOrEmpty(backupsPath),
        });
    }

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

    [SuperAdminOnly]
    [HttpDelete("users/{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken ct) =>
        ToActionResult(await sender.Send(new DeleteUserCommand(id), ct));

    [SuperAdminOnly]
    [HttpPut("users/{id:guid}/role")]
    public async Task<IActionResult> ChangeUserRole(
        Guid id, [FromBody] ChangeUserRoleRequest body, CancellationToken ct) =>
        ToActionResult(await sender.Send(new ChangeUserRoleCommand(id, body.Role), ct));

    [SuperAdminOnly]
    [HttpPost("users/{id:guid}/impersonate")]
    public async Task<IActionResult> ImpersonateUser(Guid id, CancellationToken ct) =>
        ToActionResult(await sender.Send(new ImpersonateUserCommand(id), ct));

    // --- System Settings ---

    [HttpGet("settings")]
    public async Task<IActionResult> GetSystemSettings(CancellationToken ct) =>
        ToActionResult(await sender.Send(new GetSystemSettingsQuery(), ct));

    [SuperAdminOnly]
    [HttpPut("settings/{key}")]
    public async Task<IActionResult> UpdateSystemSetting(
        string key, [FromBody] UpdateSystemSettingRequest body, CancellationToken ct) =>
        ToActionResult(await sender.Send(new UpdateSystemSettingCommand(key, body.Value), ct));

    // --- Audit Log ---

    [HttpGet("audit")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? action,
        [FromQuery] Guid? adminId,
        [FromQuery] DateTimeOffset? dateFrom,
        [FromQuery] DateTimeOffset? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default) =>
        ToActionResult(await sender.Send(
            new GetAuditLogsQuery(action, adminId, dateFrom, dateTo, page, pageSize), ct));

    // --- AI Monitoring ---

    [HttpGet("ai/logs")]
    public async Task<IActionResult> GetAiLogs(
        [FromQuery] string? provider,
        [FromQuery] string? callType,
        [FromQuery] bool? success,
        [FromQuery] DateTimeOffset? dateFrom,
        [FromQuery] DateTimeOffset? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default) =>
        ToActionResult(await sender.Send(
            new GetAiLogsQuery(provider, callType, success, dateFrom, dateTo, page, pageSize), ct));

    [HttpGet("ai/stats")]
    public async Task<IActionResult> GetAiStats(
        [FromQuery] int hours = 24, CancellationToken ct = default) =>
        ToActionResult(await sender.Send(new GetAiStatsQuery(hours), ct));

    [HttpGet("ai/status")]
    public async Task<IActionResult> GetAiStatus(
        [FromQuery] int windowMinutes = 60, CancellationToken ct = default) =>
        ToActionResult(await sender.Send(new GetAiStatusQuery(windowMinutes), ct));

    // --- Language Management ---

    [HttpGet("languages")]
    public async Task<IActionResult> GetLanguages(
        [FromQuery] bool includeInactive = true, CancellationToken ct = default) =>
        ToActionResult(await sender.Send(new GetLanguagesQuery(includeInactive), ct));

    [SuperAdminOnly]
    [HttpPost("languages")]
    public async Task<IActionResult> AddLanguage(
        [FromBody] AddLanguageRequest body, CancellationToken ct) =>
        ToActionResult(await sender.Send(
            new AddLanguageCommand(body.Code, body.Name, body.NativeName, body.SortOrder), ct),
            dto => CreatedAtAction(nameof(GetLanguages), dto));

    [SuperAdminOnly]
    [HttpPut("languages/{code}/toggle")]
    public async Task<IActionResult> ToggleLanguage(string code, CancellationToken ct) =>
        ToActionResult(await sender.Send(new ToggleLanguageCommand(code), ct));
}

public sealed record ChangeUserRoleRequest(string Role);
public sealed record UpdateSystemSettingRequest(string Value);
public sealed record AddLanguageRequest(string Code, string Name, string NativeName, short SortOrder = 0);
