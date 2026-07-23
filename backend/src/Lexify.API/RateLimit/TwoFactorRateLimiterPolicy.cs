using System.Threading.RateLimiting;
using Lexify.Infrastructure.RateLimit;
using Microsoft.AspNetCore.RateLimiting;
using StackExchange.Redis;

namespace Lexify.API.RateLimit;

/// <summary>
/// A dedicated, tight budget for 2FA code verification, separate from the general "auth" window so a
/// flood of guesses can't be spread across other auth calls. Combined with the per-code attempt ceiling
/// this makes brute-forcing a 6-digit code infeasible.
/// </summary>
public sealed class TwoFactorRateLimiterPolicy : IRateLimiterPolicy<string>
{
    public const string PolicyName = "two-factor";

    private const int Limit = 10;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

        try
        {
            var redis = httpContext.RequestServices.GetService<IConnectionMultiplexer>();
            if (redis is not null && redis.IsConnected)
            {
                return RateLimitPartition.Get(
                    partitionKey: ip,
                    factory: key => new RedisSlidingWindowRateLimiter(
                        redis.GetDatabase(),
                        $"rl:2fa:{key}",
                        Limit,
                        Window));
            }
        }
        catch
        {
            // Redis unavailable — fall through to in-memory limiter
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
