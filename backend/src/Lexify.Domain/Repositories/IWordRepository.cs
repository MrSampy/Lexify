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

    /// <summary>Counts confidence-flagged words in the block (served by the partial idx_words_confidence index).</summary>
    Task<int> CountFlaggedByBlockIdAsync(Guid blockId, CancellationToken ct = default);

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
    /// Returns words due for review, ordered by next_review_at ASC. Optionally scoped to a single
    /// block. When <paramref name="cram"/> is true the due-date filter is dropped, returning every
    /// word in scope for a practice ("cram") session regardless of schedule.
    /// </summary>
    Task<IReadOnlyList<Word>> GetDueForReviewAsync(
        Guid userId, int limit = 20, Guid? blockId = null, bool cram = false, CancellationToken ct = default);

    /// <summary>
    /// Builds a scheduled review queue: due review words (previously reviewed, next_review_at in
    /// the past) ordered by next_review_at first, then up to <paramref name="newWordAllowance"/>
    /// never-reviewed words (oldest first) within the remaining <paramref name="limit"/>.
    /// </summary>
    Task<IReadOnlyList<Word>> GetReviewQueueAsync(
        Guid userId, int limit, int newWordAllowance, Guid? blockId = null, CancellationToken ct = default);

    /// <summary>
    /// Counts words available for a review session, computed in SQL: never-reviewed words and
    /// due review words. Used by dashboard stats instead of loading full entities.
    /// </summary>
    Task<DueCounts> GetDueCountsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Counts the user's words per SM-2 mastery bucket, computed in SQL (no full load).</summary>
    Task<MasteryCounts> GetMasteryCountsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Words the user keeps forgetting: leeches (lapse count at/over the threshold) and manually
    /// confidence-flagged words, worst first (most lapses, then lowest ease factor).
    /// </summary>
    Task<IReadOnlyList<ProblemWord>> GetProblemWordsAsync(
        Guid userId, int limit = 20, CancellationToken ct = default);

    /// <summary>
    /// The <c>next_review_at</c> timestamps of every previously reviewed word scheduled before
    /// <paramref name="until"/> (including overdue ones). Bounded per user; the caller buckets
    /// them into days.
    /// </summary>
    Task<IReadOnlyList<DateTimeOffset>> GetScheduledReviewTimesAsync(
        Guid userId, DateTimeOffset until, CancellationToken ct = default);

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

/// <summary>
/// Distribution of a user's words across learning maturity buckets, derived from SM-2 state:
/// New (never repeated), Learning (interval &lt; 7d), Young (7–30d), Mature (&gt; 30d).
/// </summary>
public sealed record MasteryCounts(int New, int Learning, int Young, int Mature);

/// <summary>
/// Review-session availability: <paramref name="New"/> = never-reviewed words,
/// <paramref name="ReviewDue"/> = previously reviewed words whose next review is due.
/// </summary>
public sealed record DueCounts(int New, int ReviewDue);

/// <summary>A word the user keeps forgetting, with its block for navigation.</summary>
public sealed record ProblemWord(
    Guid WordId,
    Guid BlockId,
    string BlockTitle,
    string Term,
    string Translation,
    int LapseCount,
    double EaseFactor,
    int IntervalDays,
    DateTimeOffset NextReviewAt,
    bool ConfidenceFlag);
