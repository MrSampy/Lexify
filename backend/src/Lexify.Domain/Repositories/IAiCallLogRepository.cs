using Lexify.Domain.Entities;

namespace Lexify.Domain.Repositories;

public interface IAiCallLogRepository
{
    Task AddAsync(AiCallLog log, CancellationToken ct = default);
}
