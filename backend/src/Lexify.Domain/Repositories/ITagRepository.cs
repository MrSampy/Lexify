using Lexify.Domain.Entities;

namespace Lexify.Domain.Repositories;

public interface ITagRepository
{
    Task<Tag?> GetByUserAndNameAsync(Guid userId, string name, CancellationToken ct = default);
    Task<IReadOnlyList<Tag>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetTagNamesByBlockIdAsync(Guid blockId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> GetTagNamesByBlockIdsAsync(
        IEnumerable<Guid> blockIds, CancellationToken ct = default);
    Task AddAsync(Tag tag, CancellationToken ct = default);
    Task<bool> BlockTagExistsAsync(Guid blockId, int tagId, CancellationToken ct = default);
    Task AddBlockTagAsync(BlockTag blockTag, CancellationToken ct = default);
    Task RemoveBlockTagAsync(Guid blockId, int tagId, CancellationToken ct = default);
}
