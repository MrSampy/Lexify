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
    public DateTimeOffset? LastActiveAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

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

    public void TouchActivity()
    {
        LastActiveAt = DateTimeOffset.UtcNow;
    }

    public void UpdateDisplayName(string? displayName)
    {
        var trimmed = displayName?.Trim();
        DisplayName = string.IsNullOrEmpty(trimmed) ? null : trimmed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new DomainException("Password hash cannot be empty.");
        PasswordHash = newPasswordHash;
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
