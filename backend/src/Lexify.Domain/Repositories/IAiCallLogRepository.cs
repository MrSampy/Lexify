using Lexify.Domain.Entities;

namespace Lexify.Domain.Repositories;

public interface IAiCallLogRepository
{
    Task AddAsync(AiCallLog log, CancellationToken ct = default);

    Task<(int Total, IReadOnlyList<AiCallLog> Items)> GetPagedAsync(
        string? provider, string? callType, bool? success,
        DateTimeOffset? dateFrom, DateTimeOffset? dateTo,
        int page, int pageSize, CancellationToken ct = default);

    Task<IReadOnlyList<AiCallLog>> GetSinceAsync(DateTimeOffset since, CancellationToken ct = default);

    /// <summary>
    /// Number of AI calls a user made since <paramref name="since"/>. Backs the per-user daily quota.
    /// Every provider attempt counts, including the fallbacks of one logical call and failed calls —
    /// each attempt costs money, so counting them is the conservative choice for a spend ceiling.
    /// </summary>
    Task<int> CountByUserSinceAsync(Guid userId, DateTimeOffset since, CancellationToken ct = default);
}
