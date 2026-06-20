using Microsoft.AspNetCore.Http;

namespace Lexify.API.Middleware;

/// <summary>
/// Marks the point in the pipeline where the authenticated user context becomes available.
/// <see cref="Application.Abstractions.ICurrentUserService"/> reads claims directly from
/// <see cref="IHttpContextAccessor"/>, so no additional work is required here.
/// </summary>
public sealed class CurrentUserMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context) => next(context);
}
