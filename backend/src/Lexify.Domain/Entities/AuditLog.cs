namespace Lexify.Domain.Entities;

public sealed class AuditLog
{
    public Guid Id { get; private set; }
    public Guid AdminId { get; private set; }
    public string Action { get; private set; } = default!;
    public string? TargetType { get; private set; }
    public string? TargetId { get; private set; }
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private AuditLog() { }

    public AuditLog(Guid adminId, string action, string? targetType = null,
        string? targetId = null, string? oldValue = null, string? newValue = null,
        string? ipAddress = null, string? userAgent = null)
    {
        Id = Guid.NewGuid();
        AdminId = adminId;
        Action = action;
        TargetType = targetType;
        TargetId = targetId;
        OldValue = oldValue;
        NewValue = newValue;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
