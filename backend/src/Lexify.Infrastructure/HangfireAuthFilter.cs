using Hangfire.Dashboard;
using Lexify.Domain.Entities;

namespace Lexify.Infrastructure;

public sealed class HangfireAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var http = context.GetHttpContext();
        return http?.User.Identity?.IsAuthenticated == true
            && http.User.HasClaim("role", User.Roles.Admin);
    }
}
