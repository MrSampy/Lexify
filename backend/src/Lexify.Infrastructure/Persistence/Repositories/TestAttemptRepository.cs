using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lexify.Infrastructure.Persistence.Repositories;

public sealed class TestAttemptRepository(AppDbContext context) : ITestAttemptRepository
{
    public Task<TestAttempt?> GetByIdWithAnswersAsync(Guid id, CancellationToken ct = default) =>
        context.TestAttempts
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<TestAttempt>> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await context.TestAttempts
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.StartedAt)
            .ToListAsync(ct);

    public async Task AddAsync(TestAttempt attempt, CancellationToken ct = default) =>
        await context.TestAttempts.AddAsync(attempt, ct);

    public Task<int> CountCompletedSinceAsync(Guid userId, DateTimeOffset since, CancellationToken ct = default) =>
        context.TestAttempts
            .CountAsync(a => a.UserId == userId && a.FinishedAt != null && a.FinishedAt >= since, ct);

    public Task<int> CountAnswersSinceAsync(Guid userId, DateTimeOffset since, CancellationToken ct = default) =>
        context.AttemptAnswers
            .Where(a => a.AnsweredAt >= since
                && context.TestAttempts.Any(ta => ta.Id == a.AttemptId && ta.UserId == userId))
            .CountAsync(ct);
}
