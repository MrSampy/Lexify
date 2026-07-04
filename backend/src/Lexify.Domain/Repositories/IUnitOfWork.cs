namespace Lexify.Domain.Repositories;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Runs <paramref name="action"/> inside a database transaction — use when a handler must
    /// flush intermediate changes (e.g. to obtain a DB-generated id) that must not survive
    /// a later failure in the same operation.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);
}
