using System.IdentityModel.Tokens.Jwt;
using System.Threading.RateLimiting;
using Lexify.Infrastructure.RateLimit;
using Microsoft.AspNetCore.RateLimiting;
using StackExchange.Redis;

namespace Lexify.API.RateLimit;

public sealed class AiRateLimiterPolicy : IRateLimiterPolicy<string>
{
    public const string PolicyName = "ai-format";

    private const int Limit = 10;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        // Partition per user (JWT "sub", same as TtsRateLimiterPolicy). NameIdentifier never exists
        // here — MapInboundClaims is off — so keying on it silently degraded this to a per-IP limit
        // shared by everyone behind one NAT/proxy.
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
                    $"rl:ai:{key}",
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
            context.HttpContext.Response.Headers["Retry-After"] = "60";
            return ValueTask.CompletedTask;
        };
}
