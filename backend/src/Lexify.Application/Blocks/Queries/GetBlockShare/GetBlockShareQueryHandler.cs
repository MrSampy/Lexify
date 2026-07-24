using Lexify.Application.Abstractions;
using Lexify.Application.Blocks.Common;
using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Blocks.Queries.GetBlockShare;

public sealed class GetBlockShareQueryHandler(
    IWordBlockRepository blockRepository,
    IBlockShareRepository shareRepository,
    ICurrentUserService currentUser)
    : IRequestHandler<GetBlockShareQuery, Result<BlockShareDto?>>
{
    public async Task<Result<BlockShareDto?>> Handle(
        GetBlockShareQuery request, CancellationToken cancellationToken)
    {
        var block = await blockRepository.GetByIdAsync(request.BlockId, cancellationToken);
        if (block is null)
            return Result.NotFound<BlockShareDto?>("Block not found.");

        if (block.UserId != currentUser.UserId)
            return Result.Forbidden<BlockShareDto?>("You do not have access to this block.");

        var share = await shareRepository.GetActiveByBlockIdAsync(block.Id, cancellationToken);

        return Result.Ok<BlockShareDto?>(share is null
            ? null
            : new BlockShareDto(share.Token, share.CreatedAt, share.ViewCount, share.CopyCount));
    }
}
