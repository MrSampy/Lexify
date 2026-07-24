using System.Threading.RateLimiting;
using Lexify.Infrastructure.RateLimit;
using Microsoft.AspNetCore.RateLimiting;
using StackExchange.Redis;

namespace Lexify.API.RateLimit;

/// <summary>
/// 30 requests per 15 minutes per IP address for auth endpoints. Raised from 10 once 2FA landed: a
/// single sign-in now costs login + verify-2fa (+ the occasional resend) instead of one call, and this
/// window also covers <c>refresh</c>, so the old budget burned out after a couple of logins from one
/// address — which behind a NAT or shared office IP means several people at once.
/// </summary>
public sealed class AuthRateLimiterPolicy : IRateLimiterPolicy<string>
{
    public const string PolicyName = "auth";

    private const int Limit = 30;
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
                        $"rl:auth:{key}",
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
