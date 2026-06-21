using System.Text.Json;
using Lexify.Application.Abstractions;
using StackExchange.Redis;

namespace Lexify.Infrastructure.Services;

public sealed class RedisCacheService(IConnectionMultiplexer redis) : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private IDatabase Db => redis.GetDatabase();

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            var value = await Db.StringGetAsync(key);
            return value.HasValue ? JsonSerializer.Deserialize<T>((string)value!, JsonOptions) : default;
        }
        catch
        {
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan duration, CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(value, JsonOptions);
            await Db.StringSetAsync(key, json, duration);
        }
        catch
        {
            // fail-open: cache miss is acceptable
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await Db.KeyDeleteAsync(key);
        }
        catch
        {
            // fail-open
        }
    }
}
