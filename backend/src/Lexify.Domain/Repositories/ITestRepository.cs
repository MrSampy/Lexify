using Lexify.Domain.Entities;

namespace Lexify.Domain.Repositories;

public interface ITestRepository
{
    Task<Test?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns the test with its questions pre-loaded.</summary>
    Task<Test?> GetByIdWithQuestionsAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Test>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(Test test, CancellationToken ct = default);
    Task AddTestBlocksAsync(IEnumerable<TestBlock> blocks, CancellationToken ct = default);
    Task UpdateAsync(Test test, CancellationToken ct = default);
}
