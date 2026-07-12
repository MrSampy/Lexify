namespace Lexify.Domain.Entities;

public sealed class PasswordResetToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private PasswordResetToken() { }

    public PasswordResetToken(Guid userId, string tokenHash, DateTimeOffset expiresAt)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public bool IsActive => UsedAt is null && ExpiresAt > DateTimeOffset.UtcNow;

    public void MarkUsed()
    {
        UsedAt = DateTimeOffset.UtcNow;
    }
}
