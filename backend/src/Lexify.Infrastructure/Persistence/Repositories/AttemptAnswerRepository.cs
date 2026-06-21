using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lexify.Infrastructure.Persistence.Repositories;

public sealed class AttemptAnswerRepository(AppDbContext context) : IAttemptAnswerRepository
{
    public async Task<IReadOnlyList<AttemptAnswer>> GetByAttemptIdAsync(
        Guid attemptId, CancellationToken ct = default) =>
        await context.AttemptAnswers
            .Where(a => a.AttemptId == attemptId)
            .ToListAsync(ct);

    public async Task AddAsync(AttemptAnswer answer, CancellationToken ct = default) =>
        await context.AttemptAnswers.AddAsync(answer, ct);
}
