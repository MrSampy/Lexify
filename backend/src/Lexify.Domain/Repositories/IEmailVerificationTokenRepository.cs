using Lexify.Domain.Entities;

namespace Lexify.Domain.Repositories;

public interface IEmailVerificationTokenRepository
{
    Task<EmailVerificationToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>The user's still-valid pending email change, if any — surfaced in the profile.</summary>
    Task<EmailVerificationToken?> GetActiveEmailChangeAsync(Guid userId, CancellationToken ct = default);

    Task AddAsync(EmailVerificationToken token, CancellationToken ct = default);

    /// <summary>Marks the user's still-active tokens of this purpose as used — a new link supersedes old ones.</summary>
    Task InvalidateActiveForUserAsync(Guid userId, string purpose, CancellationToken ct = default);

    Task<int> DeleteExpiredAsync(CancellationToken ct = default);
}
