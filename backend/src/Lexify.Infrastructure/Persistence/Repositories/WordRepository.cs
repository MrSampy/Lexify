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

    public Task<int> CountFlaggedByBlockIdAsync(Guid blockId, CancellationToken ct = default) =>
        context.Words.CountAsync(w => w.BlockId == blockId && w.ConfidenceFlag, ct);

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
        Guid userId, int limit = 20, Guid? blockId = null, bool cram = false, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var query = context.Words
            .Where(w => context.WordBlocks.Any(wb => wb.Id == w.BlockId && wb.UserId == userId));

        if (blockId.HasValue)
            query = query.Where(w => w.BlockId == blockId.Value);

        // Cram = practise everything in scope regardless of schedule; otherwise only what's due.
        if (!cram)
            query = query.Where(w => w.NextReviewAt <= now);

        return await query
            .OrderBy(w => w.NextReviewAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Word>> GetReviewQueueAsync(
        Guid userId, int limit, int newWordAllowance, Guid? blockId = null, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var owned = context.Words
            .Where(w => context.WordBlocks.Any(wb => wb.Id == w.BlockId && wb.UserId == userId));

        if (blockId.HasValue)
            owned = owned.Where(w => w.BlockId == blockId.Value);

        // Scheduled reviews come first — clearing the backlog matters more than novelty.
        var reviews = await owned
            .Where(w => w.LastReviewedAt != null && w.NextReviewAt <= now)
            .OrderBy(w => w.NextReviewAt)
            .Take(limit)
            .ToListAsync(ct);

        var newTake = Math.Min(newWordAllowance, limit - reviews.Count);
        if (newTake <= 0)
            return reviews;

        var fresh = await owned
            .Where(w => w.LastReviewedAt == null)
            .OrderBy(w => w.CreatedAt)
            .Take(newTake)
            .ToListAsync(ct);

        return [.. reviews, .. fresh];
    }

    public async Task<DueCounts> GetDueCountsAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var owned = context.Words
            .Where(w => context.WordBlocks.Any(wb => wb.Id == w.BlockId && wb.UserId == userId));

        var fresh = await owned.CountAsync(w => w.LastReviewedAt == null, ct);
        var due = await owned.CountAsync(w => w.LastReviewedAt != null && w.NextReviewAt <= now, ct);

        return new DueCounts(fresh, due);
    }

    public async Task<MasteryCounts> GetMasteryCountsAsync(Guid userId, CancellationToken ct = default)
    {
        var owned = context.Words
            .Where(w => context.WordBlocks.Any(wb => wb.Id == w.BlockId && wb.UserId == userId));

        // Buckets are mutually exclusive; "New" is repetitions == 0 (never successfully recalled),
        // the rest split the maturing interval. Four indexed counts beat loading every word.
        var neu = await owned.CountAsync(w => w.Repetitions == 0, ct);
        var learning = await owned.CountAsync(w => w.Repetitions > 0 && w.IntervalDays < 7, ct);
        var young = await owned.CountAsync(w => w.Repetitions > 0 && w.IntervalDays >= 7 && w.IntervalDays <= 30, ct);
        var mature = await owned.CountAsync(w => w.Repetitions > 0 && w.IntervalDays > 30, ct);

        return new MasteryCounts(neu, learning, young, mature);
    }

    public async Task<IReadOnlyList<ProblemWord>> GetProblemWordsAsync(
        Guid userId, int limit = 20, CancellationToken ct = default) =>
        // Sort/limit on entity columns first — EF cannot translate ordering by a member of a
        // record constructed inside the query.
        await context.Words
            .Where(w => w.LapseCount >= Word.LeechThreshold || w.ConfidenceFlag)
            .Join(context.WordBlocks.Where(wb => wb.UserId == userId),
                w => w.BlockId,
                wb => wb.Id,
                (w, wb) => new { w, wb })
            .OrderByDescending(x => x.w.LapseCount)
            .ThenBy(x => x.w.EaseFactor)
            .Take(limit)
            .Select(x => new ProblemWord(
                x.w.Id, x.wb.Id, x.wb.Title, x.w.Term, x.w.Translation,
                x.w.LapseCount, x.w.EaseFactor, x.w.IntervalDays, x.w.NextReviewAt, x.w.ConfidenceFlag))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<DateTimeOffset>> GetScheduledReviewTimesAsync(
        Guid userId, DateTimeOffset until, CancellationToken ct = default) =>
        await context.Words
            .Where(w => context.WordBlocks.Any(wb => wb.Id == w.BlockId && wb.UserId == userId))
            .Where(w => w.LastReviewedAt != null && w.NextReviewAt < until)
            .Select(w => w.NextReviewAt)
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

    public async Task<IReadOnlyList<Word>> GetByIdsInBlockAsync(
        Guid blockId,
        IReadOnlyCollection<Guid> wordIds,
        CancellationToken ct = default) =>
        await context.Words
            .Where(w => w.BlockId == blockId && wordIds.Contains(w.Id))
            .ToListAsync(ct);

    public Task DeleteRangeAsync(IEnumerable<Word> words, CancellationToken ct = default)
    {
        context.Words.RemoveRange(words);
        return Task.CompletedTask;
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
        var prefixQuery = BuildPrefixTsQueryInput(trimmed);
        if (prefixQuery.Length == 0)
            return [];

        return languageId.HasValue
            ? await SearchFtsAsync(prefixQuery, userId, cappedLimit, languageId.Value, ct)
            : await SearchFtsAsync(prefixQuery, userId, cappedLimit, null, ct);
    }

    // Converts free-text user input into a prefix-matching tsquery expression (e.g. "ven blo" -> "ven:* & blo:*")
    // so that typing the start of a word (as a live-search UI does) matches, instead of requiring a whole lexeme
    // like plainto_tsquery does. Tokens are stripped to letters/digits since to_tsquery syntax errors on stray
    // punctuation/operators — unlike plainto_tsquery, it doesn't treat arbitrary input as plain text.
    private static string BuildPrefixTsQueryInput(string rawQuery) =>
        string.Join(
            " & ",
            rawQuery
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .Select(token => new string(token.Where(char.IsLetterOrDigit).ToArray()))
                .Where(token => token.Length > 0)
                .Select(token => $"{token}:*"));

    private async Task<IReadOnlyList<WordSearchResult>> SearchFtsAsync(
        string prefixQuery, Guid userId, int limit, short? languageId, CancellationToken ct)
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
                           to_tsquery('simple', immutable_unaccent({prefixQuery}))
                       ) AS "Rank"
                FROM words w
                JOIN word_blocks wb ON wb.id = w.block_id
                WHERE wb.user_id = {userId}
                  AND wb.language_id = {lang}
                  AND to_tsvector('simple', immutable_unaccent(w.term) || ' ' || immutable_unaccent(w.translation))
                      @@ to_tsquery('simple', immutable_unaccent({prefixQuery}))
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
                           to_tsquery('simple', immutable_unaccent({prefixQuery}))
                       ) AS "Rank"
                FROM words w
                JOIN word_blocks wb ON wb.id = w.block_id
                WHERE wb.user_id = {userId}
                  AND to_tsvector('simple', immutable_unaccent(w.term) || ' ' || immutable_unaccent(w.translation))
                      @@ to_tsquery('simple', immutable_unaccent({prefixQuery}))
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
