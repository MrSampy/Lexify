using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Words.Commands.BulkDeleteWords;

public sealed class BulkDeleteWordsCommandHandler(
    IWordRepository wordRepository,
    IWordBlockRepository blockRepository,
    ICurrentUserService currentUser)
    : IRequestHandler<BulkDeleteWordsCommand, Result<int>>
{
    public async Task<Result<int>> Handle(BulkDeleteWordsCommand request, CancellationToken cancellationToken)
    {
        var block = await blockRepository.GetByIdAsync(request.BlockId, cancellationToken);
        if (block is null)
            return Result.NotFound<int>("Block not found.");

        if (block.UserId != currentUser.UserId)
            return Result.Forbidden<int>("You do not have access to this block.");

        // Scoped to the block — ids that belong to other blocks are silently dropped.
        var words = await wordRepository.GetByIdsInBlockAsync(
            request.BlockId, request.WordIds.Distinct().ToArray(), cancellationToken);

        if (words.Count == 0)
            return Result.Ok(0);

        await wordRepository.DeleteRangeAsync(words, cancellationToken);

        return Result.Ok(words.Count);
    }
}
