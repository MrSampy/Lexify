using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Words.Commands.DeleteWord;

public sealed class DeleteWordCommandHandler(
    IWordRepository wordRepository,
    IWordBlockRepository blockRepository,
    ICurrentUserService currentUser)
    : IRequestHandler<DeleteWordCommand, Result>
{
    public async Task<Result> Handle(DeleteWordCommand request, CancellationToken cancellationToken)
    {
        var word = await wordRepository.GetByIdAsync(request.WordId, cancellationToken);
        if (word is null)
            return Result.NotFound("Word not found.");

        var block = await blockRepository.GetByIdAsync(word.BlockId, cancellationToken);
        if (block is null || block.UserId != currentUser.UserId)
            return Result.Forbidden("You do not have access to this word.");

        await wordRepository.DeleteAsync(request.WordId, cancellationToken);

        return Result.Ok();
    }
}
