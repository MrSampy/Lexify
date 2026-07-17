using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Review.Commands.ReviewWord;

public sealed class ReviewWordCommandHandler(
    IWordRepository wordRepository,
    IWordBlockRepository blockRepository,
    IReviewLogRepository reviewLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReviewWordCommand, Result<RateWordResultDto>>
{
    public async Task<Result<RateWordResultDto>> Handle(ReviewWordCommand request, CancellationToken cancellationToken)
    {
        var word = await wordRepository.GetByIdAsync(request.WordId, cancellationToken);
        if (word is null)
            return Result.NotFound<RateWordResultDto>("Word not found.");

        var block = await blockRepository.GetByIdAsync(word.BlockId, cancellationToken);
        if (block is null || block.UserId != request.UserId)
            return Result.Forbidden<RateWordResultDto>("You do not have access to this word.");

        word.ApplyReviewResult(request.Quality);
        await wordRepository.UpdateAsync(word, cancellationToken);

        // Persist the review so progress/stats (streaks, heatmap, accuracy) can be reconstructed —
        // the SM-2 fields on Word are overwritten in place and keep no history of their own.
        await reviewLogRepository.AddAsync(
            new WordReviewLog(
                request.UserId, word.Id, block.Id, block.LanguageId,
                request.Quality, WordReviewLog.Sources.Review,
                word.EaseFactor, word.IntervalDays),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok(new RateWordResultDto(
            word.IntervalDays, word.NextReviewAt, word.EaseFactor, word.Repetitions, word.IsLeech));
    }
}
