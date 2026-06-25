using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using NSubstitute;
using AppSubmitAnswer = Lexify.Application.Tests.Commands.SubmitAnswer;

namespace Lexify.Application.Tests.TestHandlers;

public class SubmitAnswerCommandTests
{
    private readonly ITestAttemptRepository _attemptRepo;
    private readonly IQuestionRepository _questionRepo;
    private readonly IAttemptAnswerRepository _answerRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AppSubmitAnswer.SubmitAnswerCommandHandler _handler;

    public SubmitAnswerCommandTests()
    {
        _attemptRepo = Substitute.For<ITestAttemptRepository>();
        _questionRepo = Substitute.For<IQuestionRepository>();
        _answerRepo = Substitute.For<IAttemptAnswerRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new AppSubmitAnswer.SubmitAnswerCommandHandler(
            _attemptRepo, _questionRepo, _answerRepo, _unitOfWork);
    }

    private (TestAttempt attempt, Question question) SetupOpenAnswerScenario(
        Guid userId, string correctAnswer)
    {
        var testId = Guid.NewGuid();
        var attempt = new TestAttempt(testId, userId);
        var question = new Question(
            testId, null, Question.QuestionTypes.OpenAnswer,
            "Translate this word.", correctAnswer, 0, "hash");

        _attemptRepo.GetByIdWithAnswersAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        _questionRepo.GetByIdWithOptionsAsync(question.Id, Arg.Any<CancellationToken>()).Returns(question);

        return (attempt, question);
    }

    [Fact]
    public async Task Handle_ExactOpenAnswer_ReturnsCorrect()
    {
        var userId = Guid.NewGuid();
        var (attempt, question) = SetupOpenAnswerScenario(userId, "apple");
        var command = new AppSubmitAnswer.SubmitAnswerCommand(attempt.Id, question.Id, userId, "apple", null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsCorrect);
    }

    [Fact]
    public async Task Handle_OneTypoOpenAnswer_ReturnsCorrect()
    {
        // Levenshtein("appel", "apple") = 2 (swap el→le) ≤ 2 → correct
        var userId = Guid.NewGuid();
        var (attempt, question) = SetupOpenAnswerScenario(userId, "apple");
        var command = new AppSubmitAnswer.SubmitAnswerCommand(attempt.Id, question.Id, userId, "appel", null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsCorrect);
    }

    [Fact]
    public async Task Handle_ThreeTyposOpenAnswer_ReturnsIncorrect()
    {
        // Levenshtein("apxyz", "apple") = 3 replacements > 2 → incorrect
        var userId = Guid.NewGuid();
        var (attempt, question) = SetupOpenAnswerScenario(userId, "apple");
        var command = new AppSubmitAnswer.SubmitAnswerCommand(attempt.Id, question.Id, userId, "apxyz", null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsCorrect);
    }

    [Fact]
    public async Task Handle_CaseInsensitiveOpenAnswer_ReturnsCorrect()
    {
        var userId = Guid.NewGuid();
        var (attempt, question) = SetupOpenAnswerScenario(userId, "Apple");
        var command = new AppSubmitAnswer.SubmitAnswerCommand(attempt.Id, question.Id, userId, "apple", null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsCorrect);
    }
}
