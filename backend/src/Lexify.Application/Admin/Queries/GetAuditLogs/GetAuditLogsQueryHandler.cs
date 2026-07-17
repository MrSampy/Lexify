using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Admin.Queries.GetAuditLogs;

public sealed class GetAuditLogsQueryHandler(IAuditLogRepository auditLogRepository)
    : IRequestHandler<GetAuditLogsQuery, Result<PagedResult<AuditLogDto>>>
{
    public async Task<Result<PagedResult<AuditLogDto>>> Handle(
        GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 200);
        var page = Math.Max(1, request.Page);

        var (total, items) = await auditLogRepository.GetPagedAsync(
            request.Action, request.AdminId,
            request.DateFrom, request.DateTo,
            page, pageSize, cancellationToken);

        var dtos = items
            .Select(l => new AuditLogDto(
                l.Id, l.AdminId, l.AdminEmail, l.Action, l.TargetType, l.TargetId,
                l.OldValue, l.NewValue, l.IpAddress, l.CreatedAt))
            .ToList();

        return Result.Ok(new PagedResult<AuditLogDto>(dtos, total, page, pageSize));
    }
}
