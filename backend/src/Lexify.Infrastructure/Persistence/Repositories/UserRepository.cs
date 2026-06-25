using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lexify.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(AppDbContext context) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        context.Users.FirstOrDefaultAsync(
            u => u.Email == email.ToLowerInvariant().Trim(), ct);

    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await context.Users.AddAsync(user, ct);

    public Task UpdateAsync(User user, CancellationToken ct = default)
    {
        context.Users.Update(user);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<(string Email, int DueCount)>> GetUsersWithDueWordsAsync(
        CancellationToken ct = default)
    {
        var rows = await context.Database
            .SqlQuery<UserDueWordsRow>($"""
                SELECT u.email AS "Email", COUNT(w.id)::int AS "DueCount"
                FROM users u
                JOIN word_blocks wb ON wb.user_id = u.id
                JOIN words w ON w.block_id = wb.id AND w.next_review_at <= NOW()
                WHERE u.status = 'active' AND u.deleted_at IS NULL
                GROUP BY u.email
                HAVING COUNT(w.id) > 0
                """)
            .ToListAsync(ct);

        return rows.Select(r => (r.Email, r.DueCount)).ToList();
    }

    private sealed record UserDueWordsRow(string Email, int DueCount);
}
