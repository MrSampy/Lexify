using System.IdentityModel.Tokens.Jwt;
using Lexify.Domain.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace Lexify.API.Middleware;

/// <summary>
/// Keeps <c>users.last_active_at</c> current from ordinary API traffic, which is what the admin user
/// list means by "last active". Before this existed nothing ever wrote the column, so every account
/// showed a blank there forever.
/// </summary>
public sealed partial class LastActiveMiddleware(RequestDelegate next)
{
    /// <summary>
    /// How stale the stamp is allowed to get. One write per user per window instead of one per request:
    /// the admin list shows a date, so minute-level precision buys nothing worth an UPDATE per call.
    /// </summary>
    private static readonly TimeSpan TouchInterval = TimeSpan.FromMinutes(15);

    public async Task InvokeAsync(
        HttpContext context,
        IUserRepository userRepository,
        IMemoryCache cache,
        ILogger<LastActiveMiddleware> logger)
    {
        await next(context);

        // The JWT's "sub", never ClaimTypes.NameIdentifier: MapInboundClaims is off, so the mapped
        // claim does not exist here and reading it would silently make this a no-op.
        var sub = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (sub is null || !Guid.TryParse(sub, out var userId))
            return;

        // Process-local on purpose. ICacheService degrades to NullCacheService when Redis is absent,
        // which would turn this throttle off and write on every request; a per-instance marker only
        // costs one extra write per instance per window if the app is ever scaled out.
        var cacheKey = $"last-active:{userId}";
        if (cache.TryGetValue(cacheKey, out _))
            return;

        cache.Set(cacheKey, true, TouchInterval);

        try
        {
            // While an admin impersonates someone, the token's subject is the impersonated user, so the
            // stamp lands on them. That is the honest reading — their session is what is being used.
            // CancellationToken.None: the response is already sent, and RequestAborted may be cancelled.
            await userRepository.TouchLastActiveAsync(userId, CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Recording activity must never turn a served request into an error.
            LogTouchFailed(logger, ex, userId);
            cache.Remove(cacheKey);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Failed to update last_active_at for user {UserId}")]
    private static partial void LogTouchFailed(ILogger logger, Exception ex, Guid userId);
}
