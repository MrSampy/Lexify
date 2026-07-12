using Lexify.Domain.Entities;

namespace Lexify.Domain.Repositories;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);
    Task AddAsync(PasswordResetToken token, CancellationToken ct = default);
    /// <summary>Marks every still-active token of the user as used (a new request supersedes old links).</summary>
    Task InvalidateActiveForUserAsync(Guid userId, CancellationToken ct = default);
    Task<int> DeleteExpiredAsync(CancellationToken ct = default);
}
