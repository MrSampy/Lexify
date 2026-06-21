using Lexify.Domain.Entities;

namespace Lexify.Domain.Repositories;

public interface IWordBlockRepository
{
    Task<WordBlock?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<WordBlock?> GetByIdWithWordsAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<WordBlock>> GetByUserIdAsync(
        Guid userId,
        short? languageId = null,
        string? tag = null,
        int skip = 0,
        int take = 20,
        CancellationToken ct = default);

    Task<int> CountByUserIdAsync(
        Guid userId,
        short? languageId = null,
        string? tag = null,
        CancellationToken ct = default);

    Task AddAsync(WordBlock block, CancellationToken ct = default);
    Task UpdateAsync(WordBlock block, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
