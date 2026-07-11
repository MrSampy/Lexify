using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Words.Commands.UpdateWord;

public sealed class UpdateWordCommandHandler(
    IWordRepository wordRepository,
    IWordBlockRepository blockRepository,
    ICurrentUserService currentUser)
    : IRequestHandler<UpdateWordCommand, Result>
{
    public async Task<Result> Handle(UpdateWordCommand request, CancellationToken cancellationToken)
    {
        var word = await wordRepository.GetByIdAsync(request.WordId, cancellationToken);
        if (word is null)
            return Result.NotFound("Word not found.");

        var block = await blockRepository.GetByIdAsync(word.BlockId, cancellationToken);
        if (block is null || block.UserId != currentUser.UserId)
            return Result.Forbidden("You do not have access to this word.");

        word.UpdateDetails(request.Translation, request.Notes, request.ExampleSentence);
        word.SetConfidence(request.ConfidenceFlag, request.ConfidenceNote);
        if (request.AlternativeTranslations is not null)
            word.SetAlternativeTranslations(request.AlternativeTranslations);
        if (request.Synonyms is not null)
            word.SetSynonyms(request.Synonyms);
        await wordRepository.UpdateAsync(word, cancellationToken);

        return Result.Ok();
    }
}
