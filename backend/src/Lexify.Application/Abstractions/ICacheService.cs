namespace Lexify.Application.Abstractions;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan duration, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    /// <summary>Removes every key starting with <paramref name="prefix"/> — used to invalidate paged/filtered query caches.</summary>
    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
}
