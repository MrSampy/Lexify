using System.Text.RegularExpressions;
using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Blocks.Commands.AddTagToBlock;

public sealed class AddTagToBlockCommandHandler(
    IWordBlockRepository blockRepository,
    ITagRepository tagRepository,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AddTagToBlockCommand, Result>
{
    private static readonly Regex ValidTagName =
        new(@"^[a-z0-9а-яёіїє_-]{1,50}$", RegexOptions.Compiled);

    public async Task<Result> Handle(AddTagToBlockCommand request, CancellationToken cancellationToken)
    {
        var block = await blockRepository.GetByIdAsync(request.BlockId, cancellationToken);
        if (block is null) return Result.NotFound("Block not found.");
        if (block.UserId != currentUser.UserId) return Result.Forbidden("Access denied.");

        var normalized = request.TagName.Trim().ToLowerInvariant();
        if (!ValidTagName.IsMatch(normalized))
            return Result.Failure("Tag name must be 1-50 lowercase alphanumeric characters, underscores, or hyphens.");

        // One transaction: the intermediate flush (needed for the DB-generated SERIAL Id)
        // must not leave an orphan Tag behind if attaching it to the block fails.
        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var tag = await tagRepository.GetByUserAndNameAsync(currentUser.UserId, normalized, ct);
            if (tag is null)
            {
                tag = new Tag(currentUser.UserId, normalized);
                await tagRepository.AddAsync(tag, ct);
                await unitOfWork.SaveChangesAsync(ct);
            }

            if (!await tagRepository.BlockTagExistsAsync(request.BlockId, tag.Id, ct))
            {
                await tagRepository.AddBlockTagAsync(new BlockTag(request.BlockId, tag.Id), ct);
                await unitOfWork.SaveChangesAsync(ct);
            }
        }, cancellationToken);

        return Result.Ok();
    }
}
