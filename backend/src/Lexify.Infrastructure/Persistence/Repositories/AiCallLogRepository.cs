using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lexify.Infrastructure.Persistence.Repositories;

public sealed class AiCallLogRepository(AppDbContext context) : IAiCallLogRepository
{
    public async Task AddAsync(AiCallLog log, CancellationToken ct = default) =>
        await context.AiCallLogs.AddAsync(log, ct);

    public async Task<(int Total, IReadOnlyList<AiCallLog> Items)> GetPagedAsync(
        string? provider, string? callType, bool? success,
        DateTimeOffset? dateFrom, DateTimeOffset? dateTo,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.AiCallLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(provider))
            query = query.Where(l => l.Provider == provider);

        if (!string.IsNullOrWhiteSpace(callType))
            query = query.Where(l => l.CallType == callType);

        if (success.HasValue)
            query = query.Where(l => l.Success == success.Value);

        if (dateFrom.HasValue)
            query = query.Where(l => l.CreatedAt >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(l => l.CreatedAt <= dateTo.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (total, items);
    }

    public async Task<IReadOnlyList<AiCallLog>> GetSinceAsync(DateTimeOffset since, CancellationToken ct = default) =>
        await context.AiCallLogs
            .AsNoTracking()
            .Where(l => l.CreatedAt >= since)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(ct);

    public async Task<int> CountByUserSinceAsync(
        Guid userId, DateTimeOffset since, CancellationToken ct = default) =>
        await context.AiCallLogs
            .AsNoTracking()
            .Where(l => l.UserId == userId && l.CreatedAt >= since)
            .CountAsync(ct);
}
