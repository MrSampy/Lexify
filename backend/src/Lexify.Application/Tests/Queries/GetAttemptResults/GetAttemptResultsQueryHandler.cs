using Lexify.Application.Common;
using Lexify.Application.Tests.Dtos;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Tests.Queries.GetAttemptResults;

public sealed class GetAttemptResultsQueryHandler(
    ITestAttemptRepository attemptRepository,
    IQuestionRepository questionRepository)
    : IRequestHandler<GetAttemptResultsQuery, Result<AttemptResultDto>>
{
    public async Task<Result<AttemptResultDto>> Handle(
        GetAttemptResultsQuery request, CancellationToken cancellationToken)
    {
        var attempt = await attemptRepository.GetByIdWithAnswersAsync(request.AttemptId, cancellationToken);
        if (attempt is null)
            return Result.NotFound<AttemptResultDto>("Attempt not found.");

        if (attempt.UserId != request.UserId)
            return Result.Forbidden<AttemptResultDto>("You do not have access to this attempt.");

        if (attempt.FinishedAt is null)
            return Result.Failure<AttemptResultDto>("Attempt is not finished yet.");

        var questions = await questionRepository.GetByTestIdAsync(attempt.TestId, cancellationToken);
        var questionMap = questions.ToDictionary(q => q.Id);

        var answers = attempt.Answers
            .Select(a =>
            {
                questionMap.TryGetValue(a.QuestionId, out var q);
                return new AttemptAnswerDetailDto(
                    a.QuestionId,
                    q?.QuestionText ?? string.Empty,
                    q?.QuestionType ?? string.Empty,
                    a.GivenAnswer,
                    q?.CorrectAnswer ?? string.Empty,
                    a.IsCorrect,
                    a.TimeSpentMs);
            })
            .ToList();

        var dto = new AttemptResultDto(
            attempt.Id,
            attempt.TestId,
            attempt.StartedAt,
            attempt.FinishedAt.Value,
            attempt.Score!.Value,
            attempt.TotalQuestions!.Value,
            attempt.CorrectAnswers!.Value,
            answers);

        return Result.Ok(dto);
    }
}
