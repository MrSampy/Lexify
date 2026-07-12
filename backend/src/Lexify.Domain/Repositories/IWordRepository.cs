using Lexify.Domain.Entities;

namespace Lexify.Domain.Repositories;

public interface IWordRepository
{
    Task<Word?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Word>> GetByBlockIdAsync(
        Guid blockId,
        string? search = null,
        int skip = 0,
        int take = 50,
        CancellationToken ct = default);

    Task<int> CountByBlockIdAsync(
        Guid blockId,
        string? search = null,
        CancellationToken ct = default);

    /// <summary>
    /// Returns a random pool of words from the user's blocks for the given language,
    /// used as distractors when building test questions.
    /// </summary>
    Task<IReadOnlyList<Word>> GetDistractorPoolAsync(
        Guid userId,
        short languageId,
        int count,
        CancellationToken ct = default);

    /// <summary>
    /// Returns words whose next_review_at &lt;= now, ordered by next_review_at ASC.
    /// </summary>
    Task<IReadOnlyList<Word>> GetDueForReviewAsync(Guid userId, int limit = 20, CancellationToken ct = default);

    Task<IReadOnlyList<Word>> GetByBlockIdsAsync(IEnumerable<Guid> blockIds, CancellationToken ct = default);

    /// <summary>
    /// Full-text search across all user's words. Uses FTS (GIN index) for queries &gt;= 3 chars,
    /// ILIKE trigram fallback for shorter queries.
    /// </summary>
    Task<IReadOnlyList<WordSearchResult>> SearchAsync(
        Guid userId,
        string query,
        short? languageId = null,
        int limit = 20,
        CancellationToken ct = default);

    /// <summary>Returns only the requested words that actually belong to the block — foreign ids are dropped.</summary>
    Task<IReadOnlyList<Word>> GetByIdsInBlockAsync(
        Guid blockId,
        IReadOnlyCollection<Guid> wordIds,
        CancellationToken ct = default);

    Task AddAsync(Word word, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Word> words, CancellationToken ct = default);
    Task UpdateAsync(Word word, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task DeleteRangeAsync(IEnumerable<Word> words, CancellationToken ct = default);
}
