using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lexify.Infrastructure.Persistence.Repositories;

public sealed class TagRepository(AppDbContext context) : ITagRepository
{
    public Task<Tag?> GetByUserAndNameAsync(Guid userId, string name, CancellationToken ct = default) =>
        context.Tags.FirstOrDefaultAsync(t => t.UserId == userId && t.Name == name, ct);

    public async Task<IReadOnlyList<Tag>> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await context.Tags
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<string>> GetTagNamesByBlockIdAsync(
        Guid blockId, CancellationToken ct = default) =>
        await context.BlockTags
            .Where(bt => bt.BlockId == blockId)
            .Join(context.Tags, bt => bt.TagId, t => t.Id, (_, t) => t.Name)
            .OrderBy(name => name)
            .ToListAsync(ct);

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> GetTagNamesByBlockIdsAsync(
        IEnumerable<Guid> blockIds, CancellationToken ct = default)
    {
        var ids = blockIds.ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, IReadOnlyList<string>>();

        var rows = await context.BlockTags
            .Where(bt => ids.Contains(bt.BlockId))
            .Join(context.Tags, bt => bt.TagId, t => t.Id, (bt, t) => new { bt.BlockId, t.Name })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => r.BlockId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<string>)g.Select(r => r.Name).OrderBy(n => n).ToList());
    }

    public async Task AddAsync(Tag tag, CancellationToken ct = default) =>
        await context.Tags.AddAsync(tag, ct);

    public Task<bool> BlockTagExistsAsync(Guid blockId, int tagId, CancellationToken ct = default) =>
        context.BlockTags.AnyAsync(bt => bt.BlockId == blockId && bt.TagId == tagId, ct);

    public async Task AddBlockTagAsync(BlockTag blockTag, CancellationToken ct = default) =>
        await context.BlockTags.AddAsync(blockTag, ct);

    public async Task RemoveBlockTagAsync(Guid blockId, int tagId, CancellationToken ct = default)
    {
        var existing = await context.BlockTags
            .FirstOrDefaultAsync(bt => bt.BlockId == blockId && bt.TagId == tagId, ct);
        if (existing is not null)
            context.BlockTags.Remove(existing);
    }
}
