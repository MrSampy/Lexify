using Lexify.Domain.Repositories;

namespace Lexify.Infrastructure.Persistence;

public sealed class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        context.SaveChangesAsync(ct);
}
