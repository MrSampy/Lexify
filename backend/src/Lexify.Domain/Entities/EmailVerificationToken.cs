using Lexify.Domain.Common;

namespace Lexify.Domain.Entities;

/// <summary>
/// A single-use link sent to an email address to prove the recipient controls it. Mirrors
/// <see cref="PasswordResetToken"/> — only the SHA-256 hash is stored, so a database leak cannot be
/// replayed — and adds a <see cref="Purpose"/> so the same table serves both sign-up confirmation and
/// changing the address on an existing account.
/// </summary>
public sealed class EmailVerificationToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public string Purpose { get; private set; } = default!;

    /// <summary>The address being moved to. Set only for <see cref="Purposes.EmailChange"/>.</summary>
    public string? NewEmail { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private EmailVerificationToken() { }

    public EmailVerificationToken(
        Guid userId, string tokenHash, string purpose, DateTimeOffset expiresAt, string? newEmail = null)
    {
        if (!Purposes.All.Contains(purpose))
            throw new DomainException($"Unknown verification purpose '{purpose}'.");

        // The whole point of an email-change token is the address it carries; without one, confirming
        // the link would silently do nothing.
        if (purpose == Purposes.EmailChange && string.IsNullOrWhiteSpace(newEmail))
            throw new DomainException("An email-change token must carry the new address.");
        if (purpose == Purposes.Signup && newEmail is not null)
            throw new DomainException("A signup token must not carry a new address.");

        Id = Guid.NewGuid();
        UserId = userId;
        TokenHash = tokenHash;
        Purpose = purpose;
        NewEmail = newEmail?.ToLowerInvariant().Trim();
        ExpiresAt = expiresAt;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public bool IsActive => UsedAt is null && ExpiresAt > DateTimeOffset.UtcNow;

    public void MarkUsed() => UsedAt = DateTimeOffset.UtcNow;

    public static class Purposes
    {
        public const string Signup = "signup";
        public const string EmailChange = "email_change";

        public static readonly IReadOnlySet<string> All = new HashSet<string> { Signup, EmailChange };
    }
}
