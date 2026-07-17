using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lexify.Infrastructure.Persistence.Repositories;

public sealed class AuditLogRepository(AppDbContext context) : IAuditLogRepository
{
    public async Task AddAsync(AuditLog log, CancellationToken ct = default) =>
        await context.AuditLogs.AddAsync(log, ct);

    public async Task<(int Total, IReadOnlyList<AuditLogEntry> Items)> GetPagedAsync(
        string? action, Guid? adminId,
        DateTimeOffset? dateFrom, DateTimeOffset? dateTo,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(l => l.Action == action);

        if (adminId.HasValue)
            query = query.Where(l => l.AdminId == adminId.Value);

        if (dateFrom.HasValue)
            query = query.Where(l => l.CreatedAt >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(l => l.CreatedAt <= dateTo.Value);

        var total = await query.CountAsync(ct);

        // Left join: the acting admin may have been deleted since; the row still matters.
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new AuditLogEntry(
                l.Id, l.AdminId,
                context.Users.Where(u => u.Id == l.AdminId).Select(u => u.Email).FirstOrDefault(),
                l.Action, l.TargetType, l.TargetId,
                l.OldValue, l.NewValue, l.IpAddress, l.CreatedAt))
            .ToListAsync(ct);

        return (total, items);
    }
}
