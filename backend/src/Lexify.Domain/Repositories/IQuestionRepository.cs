using Lexify.Domain.Entities;

namespace Lexify.Domain.Repositories;

public interface IQuestionRepository
{
    Task<IReadOnlyList<Question>> GetByTestIdAsync(Guid testId, CancellationToken ct = default);

    /// <summary>Returns the set of content_hash values already used in tests belonging to this user.</summary>
    Task<IReadOnlySet<string>> GetUsedContentHashesByUserAsync(Guid userId, CancellationToken ct = default);

    Task<Question?> GetByIdWithOptionsAsync(Guid id, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Question> questions, CancellationToken ct = default);
    Task AddOptionsRangeAsync(IEnumerable<QuestionOption> options, CancellationToken ct = default);
}
