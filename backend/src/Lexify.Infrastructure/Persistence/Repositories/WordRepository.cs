using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lexify.Infrastructure.Persistence.Repositories;

public sealed class WordRepository(AppDbContext context) : IWordRepository
{
    public Task<Word?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        context.Words.FirstOrDefaultAsync(w => w.Id == id, ct);

    public async Task<IReadOnlyList<Word>> GetByBlockIdAsync(
        Guid blockId,
        string? search = null,
        int skip = 0,
        int take = 50,
        CancellationToken ct = default)
    {
        var query = BuildBlockQuery(blockId, search);
        return await query
            .OrderBy(w => w.SortOrder)
            .ThenByDescending(w => w.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }

    public Task<int> CountByBlockIdAsync(
        Guid blockId,
        string? search = null,
        CancellationToken ct = default) =>
        BuildBlockQuery(blockId, search).CountAsync(ct);

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

    public async Task<IReadOnlyList<Word>> GetByBlockIdsAsync(
        IEnumerable<Guid> blockIds,
        CancellationToken ct = default)
    {
        var ids = blockIds.ToList();
        return await context.Words
            .Where(w => ids.Contains(w.BlockId))
            .OrderBy(w => w.BlockId)
            .ThenBy(w => w.SortOrder)
            .ToListAsync(ct);
    }

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

    public async Task<IReadOnlyList<WordSearchResult>> SearchAsync(
        Guid userId,
        string query,
        short? languageId = null,
        int limit = 20,
        CancellationToken ct = default)
    {
        var trimmed = query.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return [];

        var cappedLimit = Math.Min(limit, 50);

        if (trimmed.Length < 3)
        {
            // Trigram ILIKE fallback for very short queries
            var pattern = $"%{trimmed}%";
            return await context.Words
                .Where(w =>
                    context.WordBlocks.Any(wb =>
                        wb.Id == w.BlockId &&
                        wb.UserId == userId &&
                        (languageId == null || wb.LanguageId == languageId)) &&
                    (EF.Functions.ILike(w.Term, pattern) ||
                     EF.Functions.ILike(w.Translation, pattern)))
                .OrderBy(w => w.Term)
                .Take(cappedLimit)
                .Join(context.WordBlocks,
                    w => w.BlockId,
                    wb => wb.Id,
                    (w, wb) => new WordSearchResult(w.Id, wb.Id, wb.Title, w.Term, w.Translation, w.WordType, 1.0))
                .ToListAsync(ct);
        }

        // Full-text search via raw SQL to leverage idx_words_fts GIN index
        return languageId.HasValue
            ? await SearchFtsAsync(trimmed, userId, cappedLimit, languageId.Value, ct)
            : await SearchFtsAsync(trimmed, userId, cappedLimit, null, ct);
    }

    private async Task<IReadOnlyList<WordSearchResult>> SearchFtsAsync(
        string query, Guid userId, int limit, short? languageId, CancellationToken ct)
    {
        IQueryable<WordSearchResult> q;
        if (languageId.HasValue)
        {
            var lang = languageId.Value;
            q = context.Database.SqlQuery<WordSearchResult>($"""
                SELECT w.id AS "WordId",
                       wb.id AS "BlockId",
                       wb.title AS "BlockTitle",
                       w.term AS "Term",
                       w.translation AS "Translation",
                       w.word_type AS "WordType",
                       ts_rank(
                           to_tsvector('simple', immutable_unaccent(w.term) || ' ' || immutable_unaccent(w.translation)),
                           plainto_tsquery('simple', immutable_unaccent({query}))
                       ) AS "Rank"
                FROM words w
                JOIN word_blocks wb ON wb.id = w.block_id
                WHERE wb.user_id = {userId}
                  AND wb.language_id = {lang}
                  AND to_tsvector('simple', immutable_unaccent(w.term) || ' ' || immutable_unaccent(w.translation))
                      @@ plainto_tsquery('simple', immutable_unaccent({query}))
                ORDER BY "Rank" DESC
                LIMIT {limit}
                """);
        }
        else
        {
            q = context.Database.SqlQuery<WordSearchResult>($"""
                SELECT w.id AS "WordId",
                       wb.id AS "BlockId",
                       wb.title AS "BlockTitle",
                       w.term AS "Term",
                       w.translation AS "Translation",
                       w.word_type AS "WordType",
                       ts_rank(
                           to_tsvector('simple', immutable_unaccent(w.term) || ' ' || immutable_unaccent(w.translation)),
                           plainto_tsquery('simple', immutable_unaccent({query}))
                       ) AS "Rank"
                FROM words w
                JOIN word_blocks wb ON wb.id = w.block_id
                WHERE wb.user_id = {userId}
                  AND to_tsvector('simple', immutable_unaccent(w.term) || ' ' || immutable_unaccent(w.translation))
                      @@ plainto_tsquery('simple', immutable_unaccent({query}))
                ORDER BY "Rank" DESC
                LIMIT {limit}
                """);
        }

        return await q.ToListAsync(ct);
    }

    private IQueryable<Word> BuildBlockQuery(Guid blockId, string? search)
    {
        IQueryable<Word> query = context.Words.Where(w => w.BlockId == blockId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(w =>
                EF.Functions.ILike(w.Term, $"%{search}%") ||
                EF.Functions.ILike(w.Translation, $"%{search}%"));

        return query;
    }
}
