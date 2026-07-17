using System.Security.Claims;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;

namespace Lexify.API.Middleware;

/// <summary>
/// When the <c>maintenance.enabled</c> system setting is "true", answers 503 to every API request
/// except sign-in/refresh (so an admin can get in to turn it off) and health checks. Admins pass
/// through and can keep using the app while it's closed.
/// </summary>
public sealed class MaintenanceModeMiddleware(RequestDelegate next)
{
    private static readonly string[] OpenPrefixes = ["/api/auth", "/api/health"];

    public async Task InvokeAsync(HttpContext context, ISystemSettingRepository settingRepository)
    {
        var path = context.Request.Path;
        if (!path.StartsWithSegments("/api") ||
            OpenPrefixes.Any(p => path.StartsWithSegments(p)))
        {
            await next(context);
            return;
        }

        var setting = await settingRepository.GetByKeyAsync(
            SystemSetting.Keys.MaintenanceEnabled, context.RequestAborted);
        var enabled = setting is not null && bool.TryParse(setting.Value, out var on) && on;
        if (!enabled)
        {
            await next(context);
            return;
        }

        var isAdmin = context.User.FindFirstValue(ClaimTypes.Role) == User.Roles.Admin;
        if (isAdmin)
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await context.Response.WriteAsJsonAsync(new
        {
            error = "The service is temporarily down for maintenance. Please try again later.",
        });
    }
}
