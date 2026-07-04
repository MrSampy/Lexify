using Lexify.Domain.Entities;

namespace Lexify.Domain.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);
    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    Task UpdateAsync(RefreshToken token, CancellationToken ct = default);
    Task<int> DeleteExpiredAsync(CancellationToken ct = default);
    /// <summary>Revokes every active refresh token of the user (e.g. after a password change).</summary>
    Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default);
}
