using Lexify.Domain.Entities;

namespace Lexify.Domain.Repositories;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log, CancellationToken ct = default);

    /// <summary>Paged audit entries (with the acting admin's email), newest first, with optional filters.</summary>
    Task<(int Total, IReadOnlyList<AuditLogEntry> Items)> GetPagedAsync(
        string? action, Guid? adminId,
        DateTimeOffset? dateFrom, DateTimeOffset? dateTo,
        int page, int pageSize, CancellationToken ct = default);
}

/// <summary>An audit log row enriched with the acting admin's email for display.</summary>
public sealed record AuditLogEntry(
    Guid Id,
    Guid AdminId,
    string? AdminEmail,
    string Action,
    string? TargetType,
    string? TargetId,
    string? OldValue,
    string? NewValue,
    string? IpAddress,
    DateTimeOffset CreatedAt);
