using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Blocks.Commands.DeleteBlock;

public sealed class DeleteBlockCommandHandler(
    IWordBlockRepository blockRepository,
    ICurrentUserService currentUser)
    : IRequestHandler<DeleteBlockCommand, Result>
{
    public async Task<Result> Handle(DeleteBlockCommand request, CancellationToken cancellationToken)
    {
        var block = await blockRepository.GetByIdAsync(request.BlockId, cancellationToken);
        if (block is null)
            return Result.NotFound("Block not found.");

        if (block.UserId != currentUser.UserId)
            return Result.Forbidden("You do not have access to this block.");

        await blockRepository.DeleteAsync(request.BlockId, cancellationToken);

        return Result.Ok();
    }
}
