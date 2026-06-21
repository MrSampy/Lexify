using Lexify.Application.Abstractions;

namespace Lexify.Infrastructure.Services;

/// <summary>No-op cache used when Redis is not configured.</summary>
public sealed class NullCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) =>
        Task.FromResult<T?>(default);

    public Task SetAsync<T>(string key, T value, TimeSpan duration, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task RemoveAsync(string key, CancellationToken ct = default) =>
        Task.CompletedTask;
}
