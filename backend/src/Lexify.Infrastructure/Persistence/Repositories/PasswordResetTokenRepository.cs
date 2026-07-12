using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lexify.Infrastructure.Persistence.Repositories;

public sealed class PasswordResetTokenRepository(AppDbContext context) : IPasswordResetTokenRepository
{
    public Task<PasswordResetToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default) =>
        context.PasswordResetTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task AddAsync(PasswordResetToken token, CancellationToken ct = default) =>
        await context.PasswordResetTokens.AddAsync(token, ct);

    public async Task InvalidateActiveForUserAsync(Guid userId, CancellationToken ct = default) =>
        await context.PasswordResetTokens
            .Where(t => t.UserId == userId && t.UsedAt == null && t.ExpiresAt > DateTimeOffset.UtcNow)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAt, DateTimeOffset.UtcNow), ct);

    public Task<int> DeleteExpiredAsync(CancellationToken ct = default) =>
        context.PasswordResetTokens
            .Where(t => t.ExpiresAt < DateTimeOffset.UtcNow || t.UsedAt != null)
            .ExecuteDeleteAsync(ct);
}
