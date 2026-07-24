using Lexify.Domain.Common;

namespace Lexify.Domain.Entities;

public sealed class User : BaseEntity
{
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string? DisplayName { get; private set; }
    public string Role { get; private set; } = default!;
    public string Status { get; private set; } = default!;
    /// <summary>CEFR level (A1..C2); null = not set, tests are generated without difficulty targeting.</summary>
    public string? EnglishLevel { get; private set; }
    /// <summary>Max never-reviewed words introduced into the review queue per UTC day (0 = none).</summary>
    public int NewWordsPerDay { get; private set; } = DefaultNewWordsPerDay;

    /// <summary>
    /// Opt-out for the daily "words are due" email (<c>SendReviewRemindersJob</c>). Defaults to on so
    /// existing accounts keep the behaviour they had; the user turns it off in their profile or via the
    /// unsubscribe link in the mail itself.
    /// </summary>
    public bool EmailRemindersEnabled { get; private set; } = true;
    /// <summary>
    /// Last time the account made an authenticated request. There is deliberately no setter method:
    /// it is stamped by a single-column write (<c>IUserRepository.TouchLastActiveAsync</c>) on ordinary
    /// API traffic, so it never loads the aggregate and never moves <see cref="BaseEntity.UpdatedAt"/>.
    /// </summary>
    public DateTimeOffset? LastActiveAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    /// <summary>
    /// When the address in <see cref="Email"/> was proven to belong to the account holder. Null = never
    /// confirmed. Kept separate from <see cref="Status"/> because the two are orthogonal: an admin must be
    /// able to suspend an unconfirmed account, and confirming one must not silently un-suspend it.
    /// </summary>
    public DateTimeOffset? EmailVerifiedAt { get; private set; }

    public bool IsEmailVerified => EmailVerifiedAt is not null;

    /// <summary>
    /// Whether the user has opted into two-factor (email code) at sign-in. Admins are forced regardless of
    /// this flag — see <see cref="IsTwoFactorMandatory"/> — so the flag only governs non-admin accounts.
    /// </summary>
    public bool TwoFactorEnabled { get; private set; }

    /// <summary>Admins must always pass a second factor; the opt-in flag cannot turn this off.</summary>
    public bool IsTwoFactorMandatory => Role == Roles.Admin;

    private User() { }

    public User(string email, string passwordHash, string? displayName = null,
        string role = Roles.User, string status = Statuses.Active)
    {
        Email = email.ToLowerInvariant().Trim();
        PasswordHash = passwordHash;
        DisplayName = displayName;
        Role = role;
        Status = status;
    }

    public static User Create(string email, string passwordHash, string? displayName = null)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new DomainException("Email cannot be empty.");
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new DomainException("Password hash cannot be empty.");
        return new User(email, passwordHash, displayName);
    }

    public void Suspend()
    {
        if (Status == Statuses.Deleted) throw new DomainException("Cannot suspend a deleted user.");
        Status = Statuses.Suspended;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Delete()
    {
        Status = Statuses.Deleted;
        DeletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Restore()
    {
        Status = Statuses.Active;
        DeletedAt = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ChangeRole(string role)
    {
        Role = role;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateDisplayName(string? displayName)
    {
        var trimmed = displayName?.Trim();
        DisplayName = string.IsNullOrEmpty(trimmed) ? null : trimmed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkEmailVerified()
    {
        // Idempotent: a double-click on the confirmation link must not move the timestamp.
        if (EmailVerifiedAt is not null) return;
        EmailVerifiedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Moves the account to a new address. Verification is cleared deliberately — the caller is expected
    /// to have proven the new address first (or to send a fresh confirmation), and an unproven address
    /// must never inherit the old one's verified state.
    /// </summary>
    public void ChangeEmail(string newEmail)
    {
        if (string.IsNullOrWhiteSpace(newEmail)) throw new DomainException("Email cannot be empty.");
        Email = newEmail.ToLowerInvariant().Trim();
        EmailVerifiedAt = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void EnableTwoFactor()
    {
        // Idempotent, matching MarkEmailVerified: re-enabling an already-on account is a no-op.
        if (TwoFactorEnabled) return;
        TwoFactorEnabled = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Turns off the opt-in flag. The admin mandate is enforced by the caller (it is orthogonal to this
    /// flag): an admin's <see cref="IsTwoFactorMandatory"/> keeps 2FA on at sign-in even with the flag off.
    /// </summary>
    public void DisableTwoFactor()
    {
        if (!TwoFactorEnabled) return;
        TwoFactorEnabled = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new DomainException("Password hash cannot be empty.");
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public const int DefaultNewWordsPerDay = 10;
    public const int MaxNewWordsPerDay = 100;

    public void SetNewWordsPerDay(int count)
    {
        if (count < 0 || count > MaxNewWordsPerDay)
            throw new DomainException($"New words per day must be between 0 and {MaxNewWordsPerDay}.");
        NewWordsPerDay = count;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetEmailReminders(bool enabled)
    {
        if (EmailRemindersEnabled == enabled) return;
        EmailRemindersEnabled = enabled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetEnglishLevel(string? level)
    {
        if (level is not null && !EnglishLevels.All.Contains(level))
            throw new DomainException($"Invalid English level '{level}'. Allowed: {string.Join(", ", EnglishLevels.All)}.");
        EnglishLevel = level;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static class EnglishLevels
    {
        public const string A1 = "A1";
        public const string A2 = "A2";
        public const string B1 = "B1";
        public const string B2 = "B2";
        public const string C1 = "C1";
        public const string C2 = "C2";

        public static readonly IReadOnlyList<string> All = [A1, A2, B1, B2, C1, C2];
    }

    public static class Roles
    {
        public const string User = "user";
        public const string Moderator = "moderator";
        public const string Admin = "admin";
    }

    public static class Statuses
    {
        public const string Active = "active";
        public const string Suspended = "suspended";
        public const string Deleted = "deleted";
    }
}
