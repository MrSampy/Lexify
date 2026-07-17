using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using Lexify.Domain.ValueObjects;
using MediatR;

namespace Lexify.Application.Tests.Commands.FinishAttempt;

public sealed class FinishAttemptCommandHandler(
    ITestAttemptRepository attemptRepository,
    IQuestionRepository questionRepository,
    IWordRepository wordRepository,
    IWordBlockRepository blockRepository,
    IReviewLogRepository reviewLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<FinishAttemptCommand, Result>
{
    // Quality fed into SM-2 for a word answered correctly across the test. Deliberately 4 ("easy"),
    // not 5 — a recognition test is weaker evidence of recall than a deliberate self-rated review.
    private const int CorrectQuality = 4;

    public async Task<Result> Handle(FinishAttemptCommand request, CancellationToken cancellationToken)
    {
        var attempt = await attemptRepository.GetByIdWithAnswersAsync(request.AttemptId, cancellationToken);
        if (attempt is null)
            return Result.NotFound("Attempt not found.");

        if (attempt.UserId != request.UserId)
            return Result.Forbidden("You do not have access to this attempt.");

        if (attempt.FinishedAt is not null)
            return Result.Failure("Attempt is already finished.");

        var answers = attempt.Answers;
        if (answers.Count == 0)
            return Result.Failure("No answers have been submitted for this attempt.");

        var correct = answers.Count(a => a.IsCorrect);
        var score = TestScore.From(correct, answers.Count);
        attempt.Finish(score);
        attempt.ClearDomainEvents();

        // Feed every answered word back into SM-2 in BOTH directions: a correct answer advances the
        // schedule (quality 4), a wrong one resets it (quality 0). A word tied to several questions
        // counts as correct only if all of them were — one miss resets it. (Previously only wrong
        // answers were applied, so tests could never advance a word's interval.)
        var questions = await questionRepository.GetByTestIdAsync(attempt.TestId, cancellationToken);
        var questionToWord = questions
            .Where(q => q.WordId.HasValue)
            .ToDictionary(q => q.Id, q => q.WordId!.Value);

        var wordAllCorrect = new Dictionary<Guid, bool>();
        foreach (var answer in answers)
        {
            if (!questionToWord.TryGetValue(answer.QuestionId, out var wordId)) continue;
            wordAllCorrect[wordId] = wordAllCorrect.TryGetValue(wordId, out var soFar)
                ? soFar && answer.IsCorrect
                : answer.IsCorrect;
        }

        var reviewLogs = new List<WordReviewLog>();
        foreach (var (wordId, allCorrect) in wordAllCorrect)
        {
            var word = await wordRepository.GetByIdAsync(wordId, cancellationToken);
            if (word is null) continue;

            var quality = allCorrect ? CorrectQuality : 0;
            word.ApplyReviewResult(quality);
            word.ClearDomainEvents();
            await wordRepository.UpdateAsync(word, cancellationToken);

            var block = await blockRepository.GetByIdAsync(word.BlockId, cancellationToken);
            if (block is not null)
                reviewLogs.Add(new WordReviewLog(
                    attempt.UserId, word.Id, block.Id, block.LanguageId,
                    quality, WordReviewLog.Sources.Test,
                    word.EaseFactor, word.IntervalDays));
        }

        if (reviewLogs.Count > 0)
            await reviewLogRepository.AddRangeAsync(reviewLogs, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
