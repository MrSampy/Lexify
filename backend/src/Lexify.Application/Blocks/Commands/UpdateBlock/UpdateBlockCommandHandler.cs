using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Blocks.Commands.UpdateBlock;

public sealed class UpdateBlockCommandHandler(
    IWordBlockRepository blockRepository,
    ICurrentUserService currentUser)
    : IRequestHandler<UpdateBlockCommand, Result>
{
    public async Task<Result> Handle(UpdateBlockCommand request, CancellationToken cancellationToken)
    {
        var block = await blockRepository.GetByIdAsync(request.BlockId, cancellationToken);
        if (block is null)
            return Result.NotFound("Block not found.");

        if (block.UserId != currentUser.UserId)
            return Result.Forbidden("You do not have access to this block.");

        block.Rename(request.Title, request.Description);
        await blockRepository.UpdateAsync(block, cancellationToken);

        return Result.Ok();
    }
}
