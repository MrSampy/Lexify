using Lexify.Application.Blocks.Common;
using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Blocks.Queries.GetSharedBlock;

public sealed class GetSharedBlockQueryHandler(
    IBlockShareRepository shareRepository,
    IWordBlockRepository blockRepository,
    IWordRepository wordRepository,
    IUserRepository userRepository)
    : IRequestHandler<GetSharedBlockQuery, Result<SharedBlockDto>>
{
    /// <summary>Matches the CSV import ceiling — a copy can never produce a block larger than that.</summary>
    public const int MaxWords = 500;

    public async Task<Result<SharedBlockDto>> Handle(
        GetSharedBlockQuery request, CancellationToken cancellationToken)
    {
        var share = await shareRepository.GetByTokenAsync(request.Token, cancellationToken);
        // A revoked link is indistinguishable from a made-up one on purpose: the response should not
        // confirm that some block once lived behind this token.
        if (share is null || !share.IsActive)
            return Result.NotFound<SharedBlockDto>("This share link is no longer available.");

        var block = await blockRepository.GetByIdAsync(share.BlockId, cancellationToken);
        if (block is null)
            return Result.NotFound<SharedBlockDto>("This share link is no longer available.");

        var owner = await userRepository.GetByIdAsync(block.UserId, cancellationToken);
        var words = await wordRepository.GetByBlockIdAsync(block.Id, null, 0, MaxWords, cancellationToken);

        share.RecordView();
        await shareRepository.UpdateAsync(share, cancellationToken);

        return Result.Ok(new SharedBlockDto(
            block.Title,
            block.Description,
            block.LanguageId,
            block.WordCount,
            owner?.DisplayName,
            [.. words.Select(w => new SharedWordDto(
                w.Term,
                w.Translation,
                w.AlternativeTranslations,
                w.Synonyms,
                w.WordType,
                w.Notes,
                w.ExampleSentence))]));
    }
}
