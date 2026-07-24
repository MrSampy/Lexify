using Lexify.Domain.Entities;

namespace Lexify.Domain.Repositories;

public interface IBlockShareRepository
{
    Task<BlockShare?> GetByTokenAsync(string token, CancellationToken ct = default);
    /// <summary>The block's live share, if sharing is currently on. Revoked links are ignored.</summary>
    Task<BlockShare?> GetActiveByBlockIdAsync(Guid blockId, CancellationToken ct = default);
    Task AddAsync(BlockShare share, CancellationToken ct = default);
    Task UpdateAsync(BlockShare share, CancellationToken ct = default);
}
