using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lexify.Infrastructure.Persistence.Repositories;

public sealed class ReviewLogRepository(AppDbContext context) : IReviewLogRepository
{
    public async Task AddAsync(WordReviewLog log, CancellationToken ct = default) =>
        await context.WordReviewLogs.AddAsync(log, ct);

    public async Task AddRangeAsync(IEnumerable<WordReviewLog> logs, CancellationToken ct = default) =>
        await context.WordReviewLogs.AddRangeAsync(logs, ct);

    public async Task<IReadOnlyList<WordReviewLog>> GetByUserSinceAsync(
        Guid userId, DateTimeOffset since, CancellationToken ct = default) =>
        await context.WordReviewLogs
            .AsNoTracking()
            .Where(l => l.UserId == userId && l.ReviewedAt >= since)
            .OrderBy(l => l.ReviewedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<WordReviewLog>> GetByWordAsync(
        Guid userId, Guid wordId, int limit = 50, CancellationToken ct = default) =>
        await context.WordReviewLogs
            .AsNoTracking()
            .Where(l => l.UserId == userId && l.WordId == wordId)
            .OrderByDescending(l => l.ReviewedAt)
            .Take(limit)
            .ToListAsync(ct);

    public Task<int> CountDistinctWordsBySourceAsync(
        Guid userId, string source, CancellationToken ct = default) =>
        context.WordReviewLogs
            .Where(l => l.UserId == userId && l.Source == source)
            .Select(l => l.WordId)
            .Distinct()
            .CountAsync(ct);

    public Task<int> CountNewWordsIntroducedSinceAsync(
        Guid userId, DateTimeOffset since, CancellationToken ct = default) =>
        // A word was "introduced" in the window iff it has a log row in the window and none before
        // it. Log rows are immutable, so this survives restarts/mid-day setting changes.
        context.WordReviewLogs
            .Where(l => l.UserId == userId && l.ReviewedAt >= since)
            .Where(l => !context.WordReviewLogs.Any(e => e.WordId == l.WordId && e.ReviewedAt < since))
            .Select(l => l.WordId)
            .Distinct()
            .CountAsync(ct);
}
