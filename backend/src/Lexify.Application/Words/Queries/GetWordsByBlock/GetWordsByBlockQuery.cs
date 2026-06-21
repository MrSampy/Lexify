using Lexify.Application.Common;
using Lexify.Application.Words.Dtos;
using MediatR;

namespace Lexify.Application.Words.Queries.GetWordsByBlock;

public sealed record GetWordsByBlockQuery(
    Guid BlockId,
    Guid UserId,
    string? Search,
    int Page = 1,
    int PageSize = 50
) : IRequest<Result<PagedResult<WordDto>>>;
