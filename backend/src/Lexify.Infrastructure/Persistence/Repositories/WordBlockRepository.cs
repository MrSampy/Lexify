using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lexify.Infrastructure.Persistence.Repositories;

public sealed class WordBlockRepository(AppDbContext context) : IWordBlockRepository
{
    public Task<WordBlock?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        context.WordBlocks.FirstOrDefaultAsync(wb => wb.Id == id, ct);

    public Task<WordBlock?> GetByIdWithWordsAsync(Guid id, CancellationToken ct = default) =>
        context.WordBlocks
            .Include(wb => wb.Words)
            .FirstOrDefaultAsync(wb => wb.Id == id, ct);

    public async Task<IReadOnlyList<WordBlock>> GetByUserIdAsync(
        Guid userId,
        short? languageId = null,
        string? tag = null,
        int skip = 0,
        int take = 20,
        CancellationToken ct = default)
    {
        var query = BuildUserQuery(userId, languageId, tag);
        return await query
            .OrderByDescending(wb => wb.UpdatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }

    public Task<int> CountByUserIdAsync(
        Guid userId,
        short? languageId = null,
        string? tag = null,
        CancellationToken ct = default) =>
        BuildUserQuery(userId, languageId, tag).CountAsync(ct);

    public async Task AddAsync(WordBlock block, CancellationToken ct = default) =>
        await context.WordBlocks.AddAsync(block, ct);

    public Task UpdateAsync(WordBlock block, CancellationToken ct = default)
    {
        context.WordBlocks.Update(block);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var block = await context.WordBlocks.FindAsync([id], ct);
        if (block is not null)
            context.WordBlocks.Remove(block);
    }

    private IQueryable<WordBlock> BuildUserQuery(Guid userId, short? languageId, string? tag)
    {
        IQueryable<WordBlock> query = context.WordBlocks.Where(wb => wb.UserId == userId);

        if (languageId.HasValue)
            query = query.Where(wb => wb.LanguageId == languageId.Value);

        if (!string.IsNullOrWhiteSpace(tag))
            query = query.Where(wb =>
                context.BlockTags
                    .Join(context.Tags, bt => bt.TagId, t => t.Id, (bt, t) => new { bt.BlockId, t.Name })
                    .Any(x => x.BlockId == wb.Id && x.Name == tag));

        return query;
    }
}
