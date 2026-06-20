using Lexify.Domain.Entities;

namespace Lexify.Domain.Repositories;

public interface IWordRepository
{
    Task<Word?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Word>> GetByBlockIdAsync(Guid blockId, int skip, int take, CancellationToken ct = default);

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

    Task AddAsync(Word word, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Word> words, CancellationToken ct = default);
    Task UpdateAsync(Word word, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
