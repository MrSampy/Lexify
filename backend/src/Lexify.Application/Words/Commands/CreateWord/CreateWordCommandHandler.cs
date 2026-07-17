using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Words.Commands.CreateWord;

public sealed class CreateWordCommandHandler(
    IWordBlockRepository blockRepository,
    IWordRepository wordRepository,
    ISystemSettingRepository settingRepository,
    ICurrentUserService currentUser)
    : IRequestHandler<CreateWordCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateWordCommand request, CancellationToken cancellationToken)
    {
        var block = await blockRepository.GetByIdAsync(request.BlockId, cancellationToken);
        if (block is null)
            return Result.NotFound<Guid>("Block not found.");

        if (block.UserId != currentUser.UserId)
            return Result.Forbidden<Guid>("You do not have access to this block.");

        var maxWords = await settingRepository.GetIntAsync(
            SystemSetting.Keys.MaxWordsPerBlock, fallback: 0, cancellationToken);
        if (maxWords > 0)
        {
            var existing = await wordRepository.CountByBlockIdAsync(request.BlockId, ct: cancellationToken);
            if (existing >= maxWords)
                return Result.Failure<Guid>($"Block word limit reached ({existing}/{maxWords}).");
        }

        var word = Word.Create(
            request.BlockId,
            request.Term,
            request.Translation,
            request.WordType,
            request.Notes,
            request.ExampleSentence,
            request.SortOrder);

        if (request.Synonyms is { Count: > 0 })
            word.SetSynonyms(request.Synonyms);

        await wordRepository.AddAsync(word, cancellationToken);

        return Result.Ok(word.Id);
    }
}
