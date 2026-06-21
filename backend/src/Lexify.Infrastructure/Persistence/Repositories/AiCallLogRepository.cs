using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;

namespace Lexify.Infrastructure.Persistence.Repositories;

public sealed class AiCallLogRepository(AppDbContext context) : IAiCallLogRepository
{
    public async Task AddAsync(AiCallLog log, CancellationToken ct = default) =>
        await context.AiCallLogs.AddAsync(log, ct);
}
