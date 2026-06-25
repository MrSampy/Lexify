using System.Reflection;
using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using NSubstitute;
using AppFinishAttempt = Lexify.Application.Tests.Commands.FinishAttempt;

namespace Lexify.Application.Tests.TestHandlers;

public class FinishAttemptCommandTests
{
    private readonly ITestAttemptRepository _attemptRepo;
    private readonly IQuestionRepository _questionRepo;
    private readonly IWordRepository _wordRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AppFinishAttempt.FinishAttemptCommandHandler _handler;

    public FinishAttemptCommandTests()
    {
        _attemptRepo = Substitute.For<ITestAttemptRepository>();
        _questionRepo = Substitute.For<IQuestionRepository>();
        _wordRepo = Substitute.For<IWordRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new AppFinishAttempt.FinishAttemptCommandHandler(
            _attemptRepo, _questionRepo, _wordRepo, _unitOfWork);
    }

    private static void InjectAnswers(TestAttempt attempt, IEnumerable<AttemptAnswer> answers)
    {
        var field = typeof(TestAttempt).GetField("_answers", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var list = (List<AttemptAnswer>)field.GetValue(attempt)!;
        list.AddRange(answers);
    }

    [Fact]
    public async Task Handle_ThreeOfFiveCorrect_ScoreIsZeroPointSix()
    {
        var userId = Guid.NewGuid();
        var attempt = new TestAttempt(Guid.NewGuid(), userId);

        InjectAnswers(attempt, [
            new AttemptAnswer(attempt.Id, Guid.NewGuid(), "a1", true),
            new AttemptAnswer(attempt.Id, Guid.NewGuid(), "a2", true),
            new AttemptAnswer(attempt.Id, Guid.NewGuid(), "a3", true),
            new AttemptAnswer(attempt.Id, Guid.NewGuid(), "a4", false),
            new AttemptAnswer(attempt.Id, Guid.NewGuid(), "a5", false),
        ]);

        _attemptRepo.GetByIdWithAnswersAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        _questionRepo.GetByTestIdAsync(attempt.TestId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Question>)[]);

        var result = await _handler.Handle(
            new AppFinishAttempt.FinishAttemptCommand(attempt.Id, userId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0.6, attempt.Score!.Value, precision: 4);
        Assert.Equal(3, attempt.CorrectAnswers);
        Assert.Equal(5, attempt.TotalQuestions);
    }

    [Fact]
    public async Task Handle_AlreadyFinishedAttempt_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var attempt = new TestAttempt(Guid.NewGuid(), userId);

        InjectAnswers(attempt, [new AttemptAnswer(attempt.Id, Guid.NewGuid(), "a", true)]);
        attempt.Finish(Domain.ValueObjects.TestScore.From(1, 1));

        _attemptRepo.GetByIdWithAnswersAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);

        var result = await _handler.Handle(
            new AppFinishAttempt.FinishAttemptCommand(attempt.Id, userId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Failure, result.Status);
    }
}
