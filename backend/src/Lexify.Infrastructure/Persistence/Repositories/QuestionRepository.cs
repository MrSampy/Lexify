using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lexify.Infrastructure.Persistence.Repositories;

public sealed class QuestionRepository(AppDbContext context) : IQuestionRepository
{
    public async Task<IReadOnlyList<Question>> GetByTestIdAsync(Guid testId, CancellationToken ct = default) =>
        await context.Questions
            .Where(q => q.TestId == testId)
            .Include(q => q.Options)
            .OrderBy(q => q.SortOrder)
            .ToListAsync(ct);

    public async Task<IReadOnlySet<string>> GetUsedContentHashesByUserAsync(
        Guid userId, CancellationToken ct = default)
    {
        var hashes = await context.Questions
            .Where(q => context.Tests.Any(t => t.Id == q.TestId && t.UserId == userId))
            .Select(q => q.ContentHash)
            .Distinct()
            .ToListAsync(ct);

        return hashes.ToHashSet();
    }

    public async Task<Question?> GetByIdWithOptionsAsync(Guid id, CancellationToken ct = default) =>
        await context.Questions
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id, ct);

    public async Task AddRangeAsync(IEnumerable<Question> questions, CancellationToken ct = default) =>
        await context.Questions.AddRangeAsync(questions, ct);

    public async Task AddOptionsRangeAsync(IEnumerable<QuestionOption> options, CancellationToken ct = default) =>
        await context.QuestionOptions.AddRangeAsync(options, ct);
}
