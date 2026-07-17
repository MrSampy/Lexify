using Lexify.Domain.Entities;

namespace Lexify.Domain.Repositories;

public interface IReviewLogRepository
{
    Task AddAsync(WordReviewLog log, CancellationToken ct = default);

    Task AddRangeAsync(IEnumerable<WordReviewLog> logs, CancellationToken ct = default);

    /// <summary>
    /// A user's review log rows since <paramref name="since"/>, ascending by time. The window is
    /// always bounded (e.g. 30–90 days), so aggregation into daily counts / streaks / accuracy is
    /// done in the query handlers rather than pushing date-bucketing into SQL.
    /// </summary>
    Task<IReadOnlyList<WordReviewLog>> GetByUserSinceAsync(
        Guid userId, DateTimeOffset since, CancellationToken ct = default);

    /// <summary>
    /// Counts distinct words whose very first review happened at or after <paramref name="since"/>
    /// (i.e. new words "introduced" in that window). Reliable because log rows are immutable.
    /// </summary>
    Task<int> CountNewWordsIntroducedSinceAsync(
        Guid userId, DateTimeOffset since, CancellationToken ct = default);

    /// <summary>
    /// One word's review history, newest first. Filtered by <paramref name="userId"/> so a foreign
    /// word id simply yields an empty list.
    /// </summary>
    Task<IReadOnlyList<WordReviewLog>> GetByWordAsync(
        Guid userId, Guid wordId, int limit = 50, CancellationToken ct = default);
}
