using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using Lexify.Domain.ValueObjects;
using MediatR;

namespace Lexify.Application.Tests.Commands.FinishAttempt;

public sealed class FinishAttemptCommandHandler(
    ITestAttemptRepository attemptRepository,
    IQuestionRepository questionRepository,
    IWordRepository wordRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<FinishAttemptCommand, Result>
{
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

        // Apply SM-2 penalty (quality = 0) for words linked to wrong answers
        var wrongQuestionIds = answers
            .Where(a => !a.IsCorrect)
            .Select(a => a.QuestionId)
            .ToHashSet();

        if (wrongQuestionIds.Count > 0)
        {
            var questions = await questionRepository.GetByTestIdAsync(attempt.TestId, cancellationToken);
            var wordIdsToUpdate = questions
                .Where(q => wrongQuestionIds.Contains(q.Id) && q.WordId.HasValue)
                .Select(q => q.WordId!.Value)
                .Distinct();

            foreach (var wordId in wordIdsToUpdate)
            {
                var word = await wordRepository.GetByIdAsync(wordId, cancellationToken);
                if (word is null) continue;
                word.ApplyReviewResult(0);
                word.ClearDomainEvents();
                await wordRepository.UpdateAsync(word, cancellationToken);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
