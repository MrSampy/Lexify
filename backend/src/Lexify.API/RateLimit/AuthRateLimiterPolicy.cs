using System.Threading.RateLimiting;
using Lexify.Infrastructure.RateLimit;
using Microsoft.AspNetCore.RateLimiting;
using StackExchange.Redis;

namespace Lexify.API.RateLimit;

/// <summary>10 requests per 15 minutes per IP address for auth endpoints.</summary>
public sealed class AuthRateLimiterPolicy : IRateLimiterPolicy<string>
{
    public const string PolicyName = "auth";

    private const int Limit = 10;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

        var redis = httpContext.RequestServices.GetService<IConnectionMultiplexer>();

        if (redis is not null)
        {
            return RateLimitPartition.Get(
                partitionKey: ip,
                factory: key => new RedisSlidingWindowRateLimiter(
                    redis.GetDatabase(),
                    $"rl:auth:{key}",
                    Limit,
                    Window));
        }

        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: ip,
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = Limit,
                Window = Window,
                SegmentsPerWindow = 5,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    }

    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected =>
        static (context, _) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.Headers["Retry-After"] = "900";
            return ValueTask.CompletedTask;
        };
}
