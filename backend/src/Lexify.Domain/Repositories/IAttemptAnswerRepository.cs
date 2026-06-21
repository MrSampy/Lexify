using Lexify.Domain.Entities;

namespace Lexify.Domain.Repositories;

public interface IAttemptAnswerRepository
{
    Task<IReadOnlyList<AttemptAnswer>> GetByAttemptIdAsync(Guid attemptId, CancellationToken ct = default);
    Task AddAsync(AttemptAnswer answer, CancellationToken ct = default);
}
