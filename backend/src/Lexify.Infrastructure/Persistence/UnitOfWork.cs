using Lexify.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lexify.Infrastructure.Persistence;

public sealed class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        context.SaveChangesAsync(ct);

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
    {
        // Execution strategy wrapper keeps this compatible with EnableRetryOnFailure if it is
        // ever turned on (raw BeginTransaction throws under a retrying strategy).
        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(ct);
            await action(ct);
            await transaction.CommitAsync(ct);
        });
    }
}
