using System.IdentityModel.Tokens.Jwt;
using System.Threading.RateLimiting;
using Lexify.Infrastructure.RateLimit;
using Microsoft.AspNetCore.RateLimiting;
using StackExchange.Redis;

namespace Lexify.API.RateLimit;

/// <summary>
/// Caps the authenticated profile endpoints that send email (email-change request, 2FA enrolment/resend)
/// so a signed-in user cannot use them to bomb an arbitrary address or spin the mail path. Generous
/// enough that a real user fixing a typo or re-requesting a code never hits it.
/// </summary>
public sealed class AccountEmailRateLimiterPolicy : IRateLimiterPolicy<string>
{
    public const string PolicyName = "account-email";

    private const int Limit = 5;
    private static readonly TimeSpan Window = TimeSpan.FromHours(1);

    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        // Partition per user (JWT "sub"). NameIdentifier never exists here — MapInboundClaims is off —
        // so keying on it would silently degrade this to a per-IP limit shared behind one NAT/proxy.
        var userId = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                     ?? httpContext.Connection.RemoteIpAddress?.ToString()
                     ?? "anonymous";

        var redis = httpContext.RequestServices.GetService<IConnectionMultiplexer>();

        if (redis is not null)
        {
            return RateLimitPartition.Get(
                partitionKey: userId,
                factory: key => new RedisSlidingWindowRateLimiter(
                    redis.GetDatabase(),
                    $"rl:account-email:{key}",
                    Limit,
                    Window));
        }

        // Fallback: in-memory sliding window when Redis is unavailable
        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: userId,
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = Limit,
                Window = Window,
                SegmentsPerWindow = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    }

    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected =>
        static (context, _) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.Headers["Retry-After"] = "3600";
            return ValueTask.CompletedTask;
        };
}
