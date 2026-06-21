using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Words.Commands.CreateWord;

public sealed class CreateWordCommandHandler(
    IWordBlockRepository blockRepository,
    IWordRepository wordRepository,
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

        var word = Word.Create(
            request.BlockId,
            request.Term,
            request.Translation,
            request.WordType,
            request.Notes,
            request.ExampleSentence,
            request.SortOrder);

        await wordRepository.AddAsync(word, cancellationToken);

        return Result.Ok(word.Id);
    }
}
