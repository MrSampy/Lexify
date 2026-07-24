using Lexify.Domain.Entities;

namespace Lexify.Domain.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);
    /// <summary>Loads a token by its id — used to follow <see cref="RefreshToken.ReplacedBy"/> after a rotation.</summary>
    Task<RefreshToken?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    Task UpdateAsync(RefreshToken token, CancellationToken ct = default);
    Task<int> DeleteExpiredAsync(CancellationToken ct = default);
    /// <summary>Revokes every active refresh token of the user (e.g. after a password change).</summary>
    Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default);
}
