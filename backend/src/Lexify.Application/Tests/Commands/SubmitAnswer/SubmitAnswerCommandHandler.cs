using Lexify.Application.Common;
using Lexify.Application.Tests.Common;
using Lexify.Application.Tests.Dtos;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Tests.Commands.SubmitAnswer;

public sealed class SubmitAnswerCommandHandler(
    ITestAttemptRepository attemptRepository,
    IQuestionRepository questionRepository,
    IAttemptAnswerRepository answerRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SubmitAnswerCommand, Result<AnswerFeedbackDto>>
{
    private static readonly char[] AnswerSeparators = ['/', ','];

    public async Task<Result<AnswerFeedbackDto>> Handle(
        SubmitAnswerCommand request, CancellationToken cancellationToken)
    {
        var attempt = await attemptRepository.GetByIdWithAnswersAsync(request.AttemptId, cancellationToken);
        if (attempt is null)
            return Result.NotFound<AnswerFeedbackDto>("Attempt not found.");

        if (attempt.UserId != request.UserId)
            return Result.Forbidden<AnswerFeedbackDto>("You do not have access to this attempt.");

        if (attempt.FinishedAt is not null)
            return Result.Failure<AnswerFeedbackDto>("Attempt is already finished.");

        if (attempt.Answers.Any(a => a.QuestionId == request.QuestionId))
            return Result.Failure<AnswerFeedbackDto>("This question has already been answered.");

        var question = await questionRepository.GetByIdWithOptionsAsync(request.QuestionId, cancellationToken);
        if (question is null)
            return Result.NotFound<AnswerFeedbackDto>("Question not found.");

        if (question.TestId != attempt.TestId)
            return Result.Failure<AnswerFeedbackDto>("Question does not belong to this test.");

        var isCorrect = CheckAnswer(question, request.GivenAnswer.Trim());
        var answer = new AttemptAnswer(attempt.Id, question.Id, request.GivenAnswer, isCorrect, request.TimeSpentMs);

        await answerRepository.AddAsync(answer, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok(new AnswerFeedbackDto(isCorrect, question.CorrectAnswer));
    }

    private static bool CheckAnswer(Question question, string given)
    {
        switch (question.QuestionType)
        {
            // listen_and_type's CorrectAnswer is the bare term; the same typo tolerance applies.
            case Question.QuestionTypes.OpenAnswer:
            case Question.QuestionTypes.ListenAndType:
                return CheckFuzzy(question.CorrectAnswer, given);

            case Question.QuestionTypes.MultiSelectTheme:
            {
                var correctTexts = question.Options
                    .Where(o => o.IsCorrect)
                    .Select(o => o.OptionText.Trim().ToLowerInvariant())
                    .Order()
                    .ToArray();

                var givenTexts = given
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => s.ToLowerInvariant())
                    .Order()
                    .ToArray();

                return correctTexts.SequenceEqual(givenTexts);
            }

            case Question.QuestionTypes.MatchingPairs:
                return CheckPairs(question, given);

            // All letters are given — typo tolerance would defeat the exercise.
            case Question.QuestionTypes.WordScramble:
                return string.Equals(given, question.CorrectAnswer.Trim(), StringComparison.OrdinalIgnoreCase);

            case Question.QuestionTypes.SentenceBuilder:
                return NormalizeSentence(given) == NormalizeSentence(question.CorrectAnswer);

            default:
            {
                // single choice: translate_to_native, translate_to_foreign, fill_in_sentence, definition_match
                var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);
                if (correctOption is not null)
                    return string.Equals(given, correctOption.OptionText.Trim(), StringComparison.OrdinalIgnoreCase);

                // fallback for questions without options
                return CheckFuzzy(question.CorrectAnswer, given);
            }
        }
    }

    /// <summary>
    /// Every option is a real "term|translation" pair; the answer is the same segments joined
    /// with ';' in any order — grading compares the two as sets.
    /// </summary>
    private static bool CheckPairs(Question question, string given)
    {
        var correctPairs = question.Options
            .Select(o => o.OptionText.Trim().ToLowerInvariant())
            .Order()
            .ToArray();

        var givenPairs = given
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => s.ToLowerInvariant())
            .Order()
            .ToArray();

        return correctPairs.SequenceEqual(givenPairs);
    }

    /// <summary>Word order is what's graded — casing, extra whitespace, and the final '.'/'!'/'?' are not.</summary>
    private static string NormalizeSentence(string sentence) =>
        string.Join(' ', sentence.Trim().ToLowerInvariant()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .TrimEnd('.', '!', '?');

    private static bool CheckFuzzy(string correctAnswer, string given)
    {
        var givenLower = given.ToLowerInvariant();
        var alternatives = correctAnswer
            .Split(AnswerSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return alternatives.Any(alt =>
        {
            var altLower = alt.ToLowerInvariant();
            return string.Equals(givenLower, altLower, StringComparison.Ordinal)
                || LevenshteinDistance.Calculate(givenLower, altLower) <= 2;
        });
    }
}
