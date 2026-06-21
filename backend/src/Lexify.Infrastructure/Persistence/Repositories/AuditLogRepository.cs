using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;

namespace Lexify.Infrastructure.Persistence.Repositories;

public sealed class AuditLogRepository(AppDbContext context) : IAuditLogRepository
{
    public async Task AddAsync(AuditLog log, CancellationToken ct = default) =>
        await context.AuditLogs.AddAsync(log, ct);
}
