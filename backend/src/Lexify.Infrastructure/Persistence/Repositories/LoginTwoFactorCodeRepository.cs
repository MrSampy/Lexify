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
    // Guarded on Attempts < MaxAttempts so N concurrent wrong guesses (all reading Attempts=4) can't each
    // bump past the ceiling — row-level locking serialises the updates, only one crosses 4→5.
    public async Task<int> IncrementAttemptsAsync(Guid codeId, CancellationToken ct = default) =>
        await context.LoginTwoFactorCodes
            .Where(c => c.Id == codeId && c.Attempts < LoginTwoFactorCode.MaxAttempts)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Attempts, c => c.Attempts + 1), ct);

    // Guarded on UsedAt == null so a code is consumed exactly once: two concurrent verifies with the same
    // correct code both match the hash, but only one wins this atomic claim (1 row); the loser sees 0.
    public async Task<int> MarkUsedAsync(Guid codeId, CancellationToken ct = default) =>
        await context.LoginTwoFactorCodes
            .Where(c => c.Id == codeId && c.UsedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.UsedAt, DateTimeOffset.UtcNow), ct);

    public Task<int> DeleteExpiredAsync(CancellationToken ct = default) =>
        context.LoginTwoFactorCodes
            .Where(c => c.ExpiresAt < DateTimeOffset.UtcNow || c.UsedAt != null)
            .ExecuteDeleteAsync(ct);
}
