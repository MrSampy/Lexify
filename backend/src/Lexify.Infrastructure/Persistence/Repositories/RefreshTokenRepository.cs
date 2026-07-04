using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lexify.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository(AppDbContext context) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default) =>
        context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default) =>
        await context.RefreshTokens.AddAsync(token, ct);

    public Task UpdateAsync(RefreshToken token, CancellationToken ct = default)
    {
        context.RefreshTokens.Update(token);
        return Task.CompletedTask;
    }

    public Task<int> DeleteExpiredAsync(CancellationToken ct = default) =>
        context.RefreshTokens
            .Where(t => t.ExpiresAt < DateTimeOffset.UtcNow && t.RevokedAt == null)
            .ExecuteDeleteAsync(ct);

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default) =>
        await context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTimeOffset.UtcNow), ct);
}
