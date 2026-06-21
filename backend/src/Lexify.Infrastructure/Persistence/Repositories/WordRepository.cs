using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lexify.Infrastructure.Persistence.Repositories;

public sealed class WordRepository(AppDbContext context) : IWordRepository
{
    public Task<Word?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        context.Words.FirstOrDefaultAsync(w => w.Id == id, ct);

    public async Task<IReadOnlyList<Word>> GetByBlockIdAsync(
        Guid blockId, int skip, int take, CancellationToken ct = default) =>
        await context.Words
            .Where(w => w.BlockId == blockId)
            .OrderBy(w => w.SortOrder)
            .ThenByDescending(w => w.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Word>> GetDistractorPoolAsync(
        Guid userId,
        short languageId,
        int count,
        CancellationToken ct = default) =>
        await context.Words
            .Where(w => context.WordBlocks
                .Any(wb => wb.Id == w.BlockId
                           && wb.UserId == userId
                           && wb.LanguageId == languageId))
            .OrderBy(_ => EF.Functions.Random())
            .Take(count)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Word>> GetDueForReviewAsync(
        Guid userId, int limit = 20, CancellationToken ct = default) =>
        await context.Words
            .Where(w => w.NextReviewAt <= DateTimeOffset.UtcNow
                        && context.WordBlocks
                            .Any(wb => wb.Id == w.BlockId && wb.UserId == userId))
            .OrderBy(w => w.NextReviewAt)
            .Take(limit)
            .ToListAsync(ct);

    public async Task AddAsync(Word word, CancellationToken ct = default) =>
        await context.Words.AddAsync(word, ct);

    public async Task AddRangeAsync(IEnumerable<Word> words, CancellationToken ct = default) =>
        await context.Words.AddRangeAsync(words, ct);

    public Task UpdateAsync(Word word, CancellationToken ct = default)
    {
        context.Words.Update(word);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var word = await context.Words.FindAsync([id], ct);
        if (word is not null)
            context.Words.Remove(word);
    }
}
