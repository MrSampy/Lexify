using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Blocks.Queries.GetBlockById;

public sealed record GetBlockByIdQuery(
    Guid BlockId,
    Guid UserId,
    int WordsPage = 1,
    int WordsPageSize = 50
) : IRequest<Result<BlockDetailDto>>;
