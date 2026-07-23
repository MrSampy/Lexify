using Lexify.Domain.Common;

namespace Lexify.Domain.Entities;

/// <summary>
/// A single-use numeric code emailed as the second factor at sign-in (and when a user confirms turning
/// 2FA on). Only the SHA-256 hash is stored, mirroring <see cref="EmailVerificationToken"/>.
/// <para>
/// Unlike a 256-bit verification token, a 6-digit code has only a million pre-images, so the hash is not
/// meaningful protection against a database leak — it exists for consistency (plaintext is never stored).
/// The real defences are the short <see cref="ExpiresAt"/>, single use, the online <see cref="MaxAttempts"/>
/// lockout, and the tight per-endpoint rate limit.
/// </para>
/// </summary>
public sealed class LoginTwoFactorCode
{
    /// <summary>Wrong guesses allowed before the code is dead — the online brute-force ceiling.</summary>
    public const int MaxAttempts = 5;

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string CodeHash { get; private set; } = default!;
    public int Attempts { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private LoginTwoFactorCode() { }

    public LoginTwoFactorCode(Guid userId, string codeHash, DateTimeOffset expiresAt)
    {
        if (string.IsNullOrWhiteSpace(codeHash))
            throw new DomainException("Code hash cannot be empty.");

        Id = Guid.NewGuid();
        UserId = userId;
        CodeHash = codeHash;
        ExpiresAt = expiresAt;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Usable only while unused, unexpired, and under the attempt ceiling.</summary>
    public bool IsActive =>
        UsedAt is null && ExpiresAt > DateTimeOffset.UtcNow && Attempts < MaxAttempts;

    public void MarkUsed() => UsedAt = DateTimeOffset.UtcNow;

    public void RegisterFailedAttempt() => Attempts++;
}
