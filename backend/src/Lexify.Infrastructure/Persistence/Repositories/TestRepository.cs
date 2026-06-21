using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lexify.Infrastructure.Persistence.Repositories;

public sealed class TestRepository(AppDbContext context) : ITestRepository
{
    public Task<Test?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        context.Tests.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<Test?> GetByIdWithQuestionsAsync(Guid id, CancellationToken ct = default) =>
        context.Tests
            .Include(t => t.Questions)
            .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<Test>> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await context.Tests
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(Test test, CancellationToken ct = default) =>
        await context.Tests.AddAsync(test, ct);

    public async Task AddTestBlocksAsync(IEnumerable<TestBlock> blocks, CancellationToken ct = default) =>
        await context.TestBlocks.AddRangeAsync(blocks, ct);

    public Task UpdateAsync(Test test, CancellationToken ct = default)
    {
        context.Tests.Update(test);
        return Task.CompletedTask;
    }
}
