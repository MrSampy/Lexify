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
}
