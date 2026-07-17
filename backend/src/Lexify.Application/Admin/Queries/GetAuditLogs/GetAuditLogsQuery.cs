using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Admin.Queries.GetAuditLogs;

public sealed record GetAuditLogsQuery(
    string? Action,
    Guid? AdminId,
    DateTimeOffset? DateFrom,
    DateTimeOffset? DateTo,
    int Page = 1,
    int PageSize = 50) : IRequest<Result<PagedResult<AuditLogDto>>>;

public sealed record AuditLogDto(
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
