using Lexify.Domain.Entities;

namespace Lexify.Domain.Repositories;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log, CancellationToken ct = default);
}
