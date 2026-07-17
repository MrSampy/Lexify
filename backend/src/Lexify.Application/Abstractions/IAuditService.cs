namespace Lexify.Application.Abstractions;

/// <summary>
/// Records an admin action into the audit log, stamping the acting admin plus the request's
/// IP address and user agent. Does not save — the calling handler's SaveChanges commits the row
/// atomically with the action itself.
/// </summary>
public interface IAuditService
{
    /// <param name="oldValueJson">Previous state as a JSON document (the column is jsonb) — encode plain text first.</param>
    /// <param name="newValueJson">New state as a JSON document (the column is jsonb) — encode plain text first.</param>
    Task LogAsync(
        string action,
        string? targetType = null,
        string? targetId = null,
        string? oldValueJson = null,
        string? newValueJson = null,
        CancellationToken ct = default);
}
