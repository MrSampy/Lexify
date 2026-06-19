using Lexify.Domain.Common;

namespace Lexify.Domain.Entities;

public sealed class User : BaseEntity
{
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string? DisplayName { get; private set; }
    public string Role { get; private set; } = default!;
    public string Status { get; private set; } = default!;
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

    public void SoftDelete()
    {
        Status = Statuses.Deleted;
        DeletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Suspend()
    {
        Status = Statuses.Suspended;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Restore()
    {
        Status = Statuses.Active;
        DeletedAt = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void TouchActivity()
    {
        LastActiveAt = DateTimeOffset.UtcNow;
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
