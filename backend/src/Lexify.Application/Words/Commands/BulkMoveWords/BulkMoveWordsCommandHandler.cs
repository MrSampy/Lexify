using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Words.Commands.BulkMoveWords;

public sealed class BulkMoveWordsCommandHandler(
    IWordRepository wordRepository,
    IWordBlockRepository blockRepository,
    ICurrentUserService currentUser)
    : IRequestHandler<BulkMoveWordsCommand, Result<int>>
{
    public async Task<Result<int>> Handle(BulkMoveWordsCommand request, CancellationToken cancellationToken)
    {
        if (request.TargetBlockId == request.BlockId)
            return Result.Failure<int>("Target block must differ from the source block.");

        var source = await blockRepository.GetByIdAsync(request.BlockId, cancellationToken);
        if (source is null)
            return Result.NotFound<int>("Block not found.");

        if (source.UserId != currentUser.UserId)
            return Result.Forbidden<int>("You do not have access to this block.");

        var target = await blockRepository.GetByIdAsync(request.TargetBlockId, cancellationToken);
        if (target is null)
            return Result.NotFound<int>("Target block not found.");

        if (target.UserId != currentUser.UserId)
            return Result.Forbidden<int>("You do not have access to the target block.");

        // Words carry SM-2 state and feed language-scoped distractor pools and tests,
        // so cross-language moves would corrupt those — forbid them outright.
        if (source.LanguageId != target.LanguageId)
            return Result.Failure<int>("Words can only be moved between blocks of the same language.");

        // Scoped to the source block — ids that belong to other blocks are silently dropped.
        var words = await wordRepository.GetByIdsInBlockAsync(
            request.BlockId, request.WordIds.Distinct().ToArray(), cancellationToken);

        if (words.Count == 0)
            return Result.Ok(0);

        foreach (var word in words)
        {
            word.MoveToBlock(request.TargetBlockId);
            await wordRepository.UpdateAsync(word, cancellationToken);
        }

        return Result.Ok(words.Count);
    }
}
