using Lexify.Domain.Entities;

namespace Lexify.Domain.Repositories;

public interface ILoginTwoFactorCodeRepository
{
    /// <summary>The user's still-usable code (unused, unexpired, under the attempt ceiling), if any.</summary>
    Task<LoginTwoFactorCode?> GetActiveForUserAsync(Guid userId, CancellationToken ct = default);

    Task AddAsync(LoginTwoFactorCode code, CancellationToken ct = default);

    /// <summary>Marks the user's still-active codes as used — a freshly issued code supersedes old ones.</summary>
    Task InvalidateActiveForUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Atomically bumps the wrong-guess counter, but only while still under the ceiling. Must persist
    /// immediately (not a tracked mutation): a failed verify returns a failing Result, and the transaction
    /// behavior skips SaveChanges on failure, so a tracked increment would be discarded and the lockout
    /// counter would never advance. Guarded on <c>Attempts &lt; MaxAttempts</c> so concurrent wrong guesses
    /// can't push the counter past the ceiling. Returns the number of rows affected.
    /// </summary>
    Task<int> IncrementAttemptsAsync(Guid codeId, CancellationToken ct = default);

    /// <summary>
    /// Atomically claims the code as used, but only if it is still unused. Returns the rows affected: 1
    /// means this caller won the claim, 0 means the code was already consumed (a concurrent verify beat
    /// it) — the caller must treat 0 as a failed verification so a code can never be spent twice.
    /// </summary>
    Task<int> MarkUsedAsync(Guid codeId, CancellationToken ct = default);

    Task<int> DeleteExpiredAsync(CancellationToken ct = default);
}
