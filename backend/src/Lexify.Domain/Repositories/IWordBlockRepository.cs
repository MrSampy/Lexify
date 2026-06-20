using Lexify.Domain.Entities;

namespace Lexify.Domain.Repositories;

public interface IWordBlockRepository
{
    Task<WordBlock?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<WordBlock>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(WordBlock block, CancellationToken ct = default);
    Task UpdateAsync(WordBlock block, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
