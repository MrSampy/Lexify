using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Blocks.Commands.CopySharedBlock;

public sealed class CopySharedBlockCommandHandler(
    IBlockShareRepository shareRepository,
    IWordBlockRepository blockRepository,
    IWordRepository wordRepository,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CopySharedBlockCommand, Result<Guid>>
{
    /// <summary>Same ceiling as the CSV importer — one path into the app shouldn't be roomier than the other.</summary>
    private const int MaxWords = 500;

    public async Task<Result<Guid>> Handle(CopySharedBlockCommand request, CancellationToken cancellationToken)
    {
        var share = await shareRepository.GetByTokenAsync(request.Token, cancellationToken);
        if (share is null || !share.IsActive)
            return Result.NotFound<Guid>("This share link is no longer available.");

        var source = await blockRepository.GetByIdAsync(share.BlockId, cancellationToken);
        if (source is null)
            return Result.NotFound<Guid>("This share link is no longer available.");

        var sourceWords = await wordRepository.GetByBlockIdAsync(
            source.Id, null, 0, MaxWords, cancellationToken);

        var copy = WordBlock.Create(
            currentUser.UserId,
            source.LanguageId,
            $"{source.Title} (copy)",
            source.Description);

        await blockRepository.AddAsync(copy, cancellationToken);
        // Flush to get the block id before creating words that reference it — same two-step as the
        // CSV importer.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var words = new List<Word>(sourceWords.Count);
        foreach (var original in sourceWords)
        {
            // Word.Create seeds fresh SM-2 state (ease 2.5, interval 1, due now). That is the point of
            // copying rather than sharing: the recipient starts learning from zero, not from wherever
            // the owner's schedule happens to be.
            var word = Word.Create(
                copy.Id,
                original.Term,
                original.Translation,
                original.WordType,
                original.Notes,
                original.ExampleSentence,
                original.SortOrder);

            // Not constructor arguments — set through the same guarded methods the editor uses.
            word.SetAlternativeTranslations(original.AlternativeTranslations);
            word.SetSynonyms(original.Synonyms);

            words.Add(word);
        }

        if (words.Count > 0)
            await wordRepository.AddRangeAsync(words, cancellationToken);

        share.RecordCopy();
        await shareRepository.UpdateAsync(share, cancellationToken);

        return Result.Ok(copy.Id);
    }
}
