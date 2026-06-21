using Lexify.Application.Admin.Dtos;
using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Admin.Queries.GetAiLogs;

public sealed record GetAiLogsQuery(
    string? Provider,
    string? CallType,
    bool? Success,
    DateTimeOffset? DateFrom,
    DateTimeOffset? DateTo,
    int Page = 1,
    int PageSize = 50) : IRequest<Result<PagedResult<AiLogDto>>>;
