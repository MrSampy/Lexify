using Lexify.Application.Admin.Dtos;
using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Admin.Queries.GetAiLogs;

public sealed class GetAiLogsQueryHandler(IAiCallLogRepository aiCallLogRepository)
    : IRequestHandler<GetAiLogsQuery, Result<PagedResult<AiLogDto>>>
{
    public async Task<Result<PagedResult<AiLogDto>>> Handle(
        GetAiLogsQuery request, CancellationToken cancellationToken)
    {
        var (total, items) = await aiCallLogRepository.GetPagedAsync(
            request.Provider, request.CallType, request.Success,
            request.DateFrom, request.DateTo,
            request.Page, request.PageSize, cancellationToken);

        var dtos = items
            .Select(l => new AiLogDto(
                l.Id, l.UserId, l.CallType, l.Provider, l.Model,
                l.InputTokens, l.OutputTokens, l.DurationMs, l.Success,
                l.ErrorMessage, l.CreatedAt))
            .ToList();

        return Result.Ok(new PagedResult<AiLogDto>(dtos, total, request.Page, request.PageSize));
    }
}
