using Lexify.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lexify.Infrastructure.Persistence.Repositories;

public sealed class AdminUserRepository(AppDbContext context) : IAdminUserRepository
{
    public async Task<(int Total, IReadOnlyList<AdminUserEntry> Items)> GetPagedWithStatsAsync(
        string? role, string? status, string? emailSearch,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(u => u.Role == role);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(u => u.Status == status);

        if (!string.IsNullOrWhiteSpace(emailSearch))
            query = query.Where(u => u.Email.Contains(emailSearch.ToLowerInvariant().Trim()));

        var total = await query.CountAsync(ct);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.DisplayName,
                u.Role,
                u.Status,
                u.LastActiveAt,
                u.CreatedAt,
                BlockCount = context.WordBlocks.Count(wb => wb.UserId == u.Id),
                WordCount = context.Words.Count(w => context.WordBlocks.Any(wb => wb.Id == w.BlockId && wb.UserId == u.Id)),
                TestCount = context.Tests.Count(t => t.UserId == u.Id)
            })
            .ToListAsync(ct);

        var entries = users
            .Select(u => new AdminUserEntry(
                u.Id, u.Email, u.DisplayName, u.Role, u.Status,
                u.LastActiveAt, u.CreatedAt, u.BlockCount, u.WordCount, u.TestCount))
            .ToList();

        return (total, entries);
    }
}
