using Lexify.Application.Abstractions;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using Microsoft.AspNetCore.Http;

namespace Lexify.Infrastructure.Services;

public sealed class AuditService(
    IAuditLogRepository auditLogRepository,
    ICurrentUserService currentUser,
    IHttpContextAccessor httpContextAccessor) : IAuditService
{
    public Task LogAsync(
        string action,
        string? targetType = null,
        string? targetId = null,
        string? oldValueJson = null,
        string? newValueJson = null,
        CancellationToken ct = default)
    {
        var http = httpContextAccessor.HttpContext;
        var userAgent = http?.Request.Headers.UserAgent.ToString();

        var log = new AuditLog(
            adminId: currentUser.UserId,
            action: action,
            targetType: targetType,
            targetId: targetId,
            oldValue: oldValueJson,
            newValue: newValueJson,
            ipAddress: http?.Connection.RemoteIpAddress?.ToString(),
            userAgent: string.IsNullOrEmpty(userAgent) ? null : userAgent);

        return auditLogRepository.AddAsync(log, ct);
    }
}
