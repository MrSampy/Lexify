using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lexify.Infrastructure.Persistence.Repositories;

public sealed class LoginTwoFactorCodeRepository(AppDbContext context)
    : ILoginTwoFactorCodeRepository
{
    public Task<LoginTwoFactorCode?> GetActiveForUserAsync(Guid userId, CancellationToken ct = default) =>
        context.LoginTwoFactorCodes
            .Where(c => c.UserId == userId
                && c.UsedAt == null
                && c.ExpiresAt > DateTimeOffset.UtcNow
                && c.Attempts < LoginTwoFactorCode.MaxAttempts)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(LoginTwoFactorCode code, CancellationToken ct = default) =>
        await context.LoginTwoFactorCodes.AddAsync(code, ct);

    public async Task InvalidateActiveForUserAsync(Guid userId, CancellationToken ct = default) =>
        await context.LoginTwoFactorCodes
            .Where(c => c.UserId == userId
                && c.UsedAt == null
                && c.ExpiresAt > DateTimeOffset.UtcNow)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.UsedAt, DateTimeOffset.UtcNow), ct);

    // ExecuteUpdate, not a tracked mutation: a failed verify returns a failing Result and TransactionBehavior
    // skips SaveChanges, so a tracked Attempts++ would be discarded and the lockout counter would never move.
    public async Task IncrementAttemptsAsync(Guid codeId, CancellationToken ct = default) =>
        await context.LoginTwoFactorCodes
            .Where(c => c.Id == codeId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Attempts, c => c.Attempts + 1), ct);

    public async Task MarkUsedAsync(Guid codeId, CancellationToken ct = default) =>
        await context.LoginTwoFactorCodes
            .Where(c => c.Id == codeId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.UsedAt, DateTimeOffset.UtcNow), ct);

    public Task<int> DeleteExpiredAsync(CancellationToken ct = default) =>
        context.LoginTwoFactorCodes
            .Where(c => c.ExpiresAt < DateTimeOffset.UtcNow || c.UsedAt != null)
            .ExecuteDeleteAsync(ct);
}
