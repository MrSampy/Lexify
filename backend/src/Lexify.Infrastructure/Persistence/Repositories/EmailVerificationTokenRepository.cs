using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lexify.Infrastructure.Persistence.Repositories;

public sealed class EmailVerificationTokenRepository(AppDbContext context)
    : IEmailVerificationTokenRepository
{
    public Task<EmailVerificationToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default) =>
        context.EmailVerificationTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public Task<EmailVerificationToken?> GetActiveEmailChangeAsync(
        Guid userId, CancellationToken ct = default) =>
        context.EmailVerificationTokens
            .AsNoTracking()
            .Where(t => t.UserId == userId
                && t.Purpose == EmailVerificationToken.Purposes.EmailChange
                && t.UsedAt == null
                && t.ExpiresAt > DateTimeOffset.UtcNow)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(EmailVerificationToken token, CancellationToken ct = default) =>
        await context.EmailVerificationTokens.AddAsync(token, ct);

    public async Task InvalidateActiveForUserAsync(
        Guid userId, string purpose, CancellationToken ct = default) =>
        await context.EmailVerificationTokens
            .Where(t => t.UserId == userId
                && t.Purpose == purpose
                && t.UsedAt == null
                && t.ExpiresAt > DateTimeOffset.UtcNow)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAt, DateTimeOffset.UtcNow), ct);

    public Task<int> DeleteExpiredAsync(CancellationToken ct = default) =>
        context.EmailVerificationTokens
            .Where(t => t.ExpiresAt < DateTimeOffset.UtcNow || t.UsedAt != null)
            .ExecuteDeleteAsync(ct);
}
