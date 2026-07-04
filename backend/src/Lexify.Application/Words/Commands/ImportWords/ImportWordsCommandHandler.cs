using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Words.Commands.ImportWords;

public sealed class ImportWordsCommandHandler(
    IWordBlockRepository blockRepository,
    IWordRepository wordRepository,
    ICurrentUserService currentUser)
    : IRequestHandler<ImportWordsCommand, Result<int>>
{
    public async Task<Result<int>> Handle(ImportWordsCommand request, CancellationToken cancellationToken)
    {
        var block = await blockRepository.GetByIdAsync(request.BlockId, cancellationToken);
        if (block is null)
            return Result.NotFound<int>("Block not found.");

        if (block.UserId != currentUser.UserId)
            return Result.Forbidden<int>("You do not have access to this block.");

        var words = request.Words.Select(item =>
        {
            var word = Word.Create(
                request.BlockId,
                item.Term,
                item.Translation,
                item.WordType,
                item.Notes,
                item.ExampleSentence,
                item.SortOrder);

            if (item.ConfidenceFlag)
                word.SetConfidence(item.ConfidenceFlag, item.ConfidenceNote);

            if (item.AlternativeTranslations is { Count: > 0 })
                word.SetAlternativeTranslations(item.AlternativeTranslations);

            return word;
        }).ToList();

        await wordRepository.AddRangeAsync(words, cancellationToken);

        return Result.Ok(words.Count);
    }
}
