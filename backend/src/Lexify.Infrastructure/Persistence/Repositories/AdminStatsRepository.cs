using Lexify.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lexify.Infrastructure.Persistence.Repositories;

public sealed class AdminStatsRepository(AppDbContext context) : IAdminStatsRepository
{
    public Task<int> CountUsersAsync(CancellationToken ct = default) =>
        context.Users.CountAsync(ct);

    public Task<int> CountActiveUsersAsync(DateTimeOffset since, CancellationToken ct = default) =>
        context.Users.CountAsync(u => u.LastActiveAt >= since, ct);

    public Task<int> CountWordsAsync(CancellationToken ct = default) =>
        context.Words.CountAsync(ct);

    public Task<int> CountWordBlocksAsync(CancellationToken ct = default) =>
        context.WordBlocks.CountAsync(ct);

    public Task<int> CountTestsAsync(CancellationToken ct = default) =>
        context.Tests.CountAsync(ct);

    public Task<int> CountAiCallsAsync(DateTimeOffset since, CancellationToken ct = default) =>
        context.AiCallLogs.CountAsync(l => l.CreatedAt >= since, ct);

    public async Task<IReadOnlyList<(string Code, string Name, int BlockCount)>> GetTopLanguagesByBlockCountAsync(
        int top, CancellationToken ct = default)
    {
        var stats = await context.WordBlocks
            .GroupBy(wb => wb.LanguageId)
            .Select(g => new { LanguageId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(top)
            .Join(context.Languages,
                stat => stat.LanguageId,
                lang => lang.Id,
                (stat, lang) => new { lang.Code, lang.Name, stat.Count })
            .ToListAsync(ct);

        return stats.Select(x => (x.Code, x.Name, x.Count)).ToList();
    }

    public async Task<IReadOnlyList<(DateOnly Date, int Count)>> GetRegistrationsByDayAsync(
        int days, CancellationToken ct = default)
    {
        var since = DateTimeOffset.UtcNow.AddDays(-days);

        // Load timestamps in-memory to avoid DateTimeOffset grouping translation issues
        var timestamps = await context.Users
            .Where(u => u.CreatedAt >= since)
            .Select(u => u.CreatedAt)
            .ToListAsync(ct);

        return timestamps
            .GroupBy(t => DateOnly.FromDateTime(t.UtcDateTime))
            .Select(g => (Date: g.Key, Count: g.Count()))
            .OrderBy(x => x.Date)
            .ToList();
    }

    public async Task<IReadOnlyList<(DateTimeOffset HourStart, int Count)>> GetAiCallsByHourAsync(
        int hours, CancellationToken ct = default)
    {
        var since = DateTimeOffset.UtcNow.AddHours(-hours);

        var timestamps = await context.AiCallLogs
            .Where(l => l.CreatedAt >= since)
            .Select(l => l.CreatedAt)
            .ToListAsync(ct);

        return timestamps
            .GroupBy(t => new DateTimeOffset(t.UtcDateTime.Year, t.UtcDateTime.Month,
                t.UtcDateTime.Day, t.UtcDateTime.Hour, 0, 0, TimeSpan.Zero))
            .Select(g => (HourStart: g.Key, Count: g.Count()))
            .OrderBy(x => x.HourStart)
            .ToList();
    }
}
