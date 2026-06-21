using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Review.Commands.ReviewWord;

public sealed class ReviewWordCommandHandler(
    IWordRepository wordRepository,
    IWordBlockRepository blockRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReviewWordCommand, Result>
{
    public async Task<Result> Handle(ReviewWordCommand request, CancellationToken cancellationToken)
    {
        var word = await wordRepository.GetByIdAsync(request.WordId, cancellationToken);
        if (word is null)
            return Result.NotFound("Word not found.");

        var block = await blockRepository.GetByIdAsync(word.BlockId, cancellationToken);
        if (block is null || block.UserId != request.UserId)
            return Result.Forbidden("You do not have access to this word.");

        word.ApplyReviewResult(request.Quality);
        await wordRepository.UpdateAsync(word, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
