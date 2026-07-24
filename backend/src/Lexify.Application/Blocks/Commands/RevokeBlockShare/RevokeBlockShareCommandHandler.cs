using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Blocks.Commands.RevokeBlockShare;

public sealed class RevokeBlockShareCommandHandler(
    IWordBlockRepository blockRepository,
    IBlockShareRepository shareRepository,
    ICurrentUserService currentUser)
    : IRequestHandler<RevokeBlockShareCommand, Result>
{
    public async Task<Result> Handle(RevokeBlockShareCommand request, CancellationToken cancellationToken)
    {
        var block = await blockRepository.GetByIdAsync(request.BlockId, cancellationToken);
        if (block is null)
            return Result.NotFound("Block not found.");

        if (block.UserId != currentUser.UserId)
            return Result.Forbidden("You do not have access to this block.");

        var share = await shareRepository.GetActiveByBlockIdAsync(block.Id, cancellationToken);
        // Already off — nothing to do, and saying so would only make the UI handle a non-problem.
        if (share is null)
            return Result.Ok();

        share.Revoke();
        await shareRepository.UpdateAsync(share, cancellationToken);

        return Result.Ok();
    }
}
