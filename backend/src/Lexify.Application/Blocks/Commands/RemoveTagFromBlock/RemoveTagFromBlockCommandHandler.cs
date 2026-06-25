using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Blocks.Commands.RemoveTagFromBlock;

public sealed class RemoveTagFromBlockCommandHandler(
    IWordBlockRepository blockRepository,
    ITagRepository tagRepository,
    ICurrentUserService currentUser)
    : IRequestHandler<RemoveTagFromBlockCommand, Result>
{
    public async Task<Result> Handle(RemoveTagFromBlockCommand request, CancellationToken cancellationToken)
    {
        var block = await blockRepository.GetByIdAsync(request.BlockId, cancellationToken);
        if (block is null) return Result.NotFound("Block not found.");
        if (block.UserId != currentUser.UserId) return Result.Forbidden("Access denied.");

        var normalized = request.TagName.Trim().ToLowerInvariant();
        var tag = await tagRepository.GetByUserAndNameAsync(currentUser.UserId, normalized, cancellationToken);
        if (tag is null) return Result.Ok(); // tag doesn't exist — nothing to remove

        await tagRepository.RemoveBlockTagAsync(request.BlockId, tag.Id, cancellationToken);
        return Result.Ok();
    }
}
