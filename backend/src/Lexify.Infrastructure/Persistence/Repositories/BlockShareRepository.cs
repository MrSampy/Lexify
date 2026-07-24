using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lexify.Infrastructure.Persistence.Repositories;

public sealed class BlockShareRepository(AppDbContext context) : IBlockShareRepository
{
    public Task<BlockShare?> GetByTokenAsync(string token, CancellationToken ct = default) =>
        context.BlockShares.FirstOrDefaultAsync(s => s.Token == token, ct);

    public Task<BlockShare?> GetActiveByBlockIdAsync(Guid blockId, CancellationToken ct = default) =>
        context.BlockShares
            .FirstOrDefaultAsync(s => s.BlockId == blockId && s.RevokedAt == null, ct);

    public async Task AddAsync(BlockShare share, CancellationToken ct = default) =>
        await context.BlockShares.AddAsync(share, ct);

    public Task UpdateAsync(BlockShare share, CancellationToken ct = default)
    {
        context.BlockShares.Update(share);
        return Task.CompletedTask;
    }
}
