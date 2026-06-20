namespace Lexify.Domain.Entities;

public sealed class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? ReplacedBy { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private RefreshToken() { }

    public RefreshToken(Guid userId, string tokenHash, DateTimeOffset expiresAt,
        string? ipAddress = null, string? userAgent = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTimeOffset.UtcNow;

    public RefreshToken Rotate(string newTokenHash, DateTimeOffset newExpiresAt,
        string? ipAddress = null, string? userAgent = null)
    {
        var replacement = new RefreshToken(UserId, newTokenHash, newExpiresAt, ipAddress, userAgent);
        ReplacedBy = replacement.Id;
        RevokedAt = DateTimeOffset.UtcNow;
        return replacement;
    }

    public void Revoke()
    {
        RevokedAt = DateTimeOffset.UtcNow;
    }
}
