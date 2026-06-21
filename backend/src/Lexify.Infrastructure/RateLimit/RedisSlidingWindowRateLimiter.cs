using System.Threading.RateLimiting;
using StackExchange.Redis;

namespace Lexify.Infrastructure.RateLimit;

public sealed class RedisSlidingWindowRateLimiter : RateLimiter
{
    private readonly IDatabase _db;
    private readonly string _key;
    private readonly int _limit;
    private readonly TimeSpan _window;

    // Lua script: removes expired entries, checks count, adds new entry atomically
    private static readonly string SlidingWindowScript = """
        local key = KEYS[1]
        local now = tonumber(ARGV[1])
        local window_start = tonumber(ARGV[2])
        local limit = tonumber(ARGV[3])
        local ttl_ms = tonumber(ARGV[4])
        redis.call('ZREMRANGEBYSCORE', key, '-inf', window_start)
        local count = redis.call('ZCARD', key)
        if count < limit then
            redis.call('ZADD', key, now, now)
            redis.call('PEXPIRE', key, ttl_ms)
            return 1
        end
        return 0
        """;

    public RedisSlidingWindowRateLimiter(IDatabase db, string key, int limit, TimeSpan window)
    {
        _db = db;
        _key = key;
        _limit = limit;
        _window = window;
    }

    public override TimeSpan? IdleDuration => null;

    protected override RateLimitLease AttemptAcquireCore(int permitCount) =>
        new PermitLease(false);

    protected override async ValueTask<RateLimitLease> AcquireAsyncCore(
        int permitCount,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var windowStart = now - (long)_window.TotalMilliseconds;
        var ttlMs = (long)_window.TotalMilliseconds + 1000;

        try
        {
            var result = (long)await _db.ScriptEvaluateAsync(
                SlidingWindowScript,
                keys: [new RedisKey(_key)],
                values: [now, windowStart, _limit, ttlMs]);

            return new PermitLease(result == 1);
        }
        catch
        {
            // On Redis failure, allow the request rather than blocking users
            return new PermitLease(true);
        }
    }

    public override RateLimiterStatistics? GetStatistics() => null;

    protected override void Dispose(bool disposing) => base.Dispose(disposing);

    private sealed class PermitLease(bool acquired) : RateLimitLease
    {
        public override bool IsAcquired => acquired;
        public override IEnumerable<string> MetadataNames => [];

        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            metadata = null;
            return false;
        }

        protected override void Dispose(bool disposing) => base.Dispose(disposing);
    }
}
