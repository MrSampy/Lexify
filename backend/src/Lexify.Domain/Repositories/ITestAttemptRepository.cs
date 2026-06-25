using Lexify.Domain.Entities;

namespace Lexify.Domain.Repositories;

public interface ITestAttemptRepository
{
    Task<TestAttempt?> GetByIdWithAnswersAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<TestAttempt>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(TestAttempt attempt, CancellationToken ct = default);
    Task<int> CountCompletedSinceAsync(Guid userId, DateTimeOffset since, CancellationToken ct = default);
    Task<int> CountAnswersSinceAsync(Guid userId, DateTimeOffset since, CancellationToken ct = default);
}
