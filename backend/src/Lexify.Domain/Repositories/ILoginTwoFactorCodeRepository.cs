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
    /// Atomically bumps the wrong-guess counter. Must persist immediately (not a tracked mutation): a
    /// failed verify returns a failing Result, and the transaction behavior skips SaveChanges on failure,
    /// so a tracked increment would be discarded and the lockout counter would never advance.
    /// </summary>
    Task IncrementAttemptsAsync(Guid codeId, CancellationToken ct = default);

    Task MarkUsedAsync(Guid codeId, CancellationToken ct = default);

    Task<int> DeleteExpiredAsync(CancellationToken ct = default);
}
