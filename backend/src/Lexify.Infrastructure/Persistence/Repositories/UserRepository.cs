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

    public Task TouchLastActiveAsync(Guid userId, CancellationToken ct = default) =>
        context.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(u => u.LastActiveAt, DateTimeOffset.UtcNow), ct);

    public async Task<IReadOnlyList<(Guid UserId, string Email, int DueCount)>> GetUsersWithDueWordsAsync(
        CancellationToken ct = default)
    {
        var rows = await context.Database
            .SqlQuery<UserDueWordsRow>($"""
                SELECT u.id AS "UserId", u.email AS "Email", COUNT(w.id)::int AS "DueCount"
                FROM users u
                JOIN word_blocks wb ON wb.user_id = u.id
                JOIN words w ON w.block_id = wb.id AND w.next_review_at <= NOW()
                WHERE u.status = 'active'
                  AND u.deleted_at IS NULL
                  AND u.email_reminders_enabled
                GROUP BY u.id, u.email
                HAVING COUNT(w.id) > 0
                """)
            .ToListAsync(ct);

        return rows.Select(r => (r.UserId, r.Email, r.DueCount)).ToList();
    }

    private sealed record UserDueWordsRow(Guid UserId, string Email, int DueCount);
}
