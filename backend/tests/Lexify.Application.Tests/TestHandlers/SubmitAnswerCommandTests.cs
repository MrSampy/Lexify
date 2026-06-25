using System.Reflection;
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

    // ---- Error paths ----

    [Fact]
    public async Task Handle_AttemptNotFound_ReturnsNotFound()
    {
        _attemptRepo.GetByIdWithAnswersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((TestAttempt?)null);

        var result = await _handler.Handle(
            new AppSubmitAnswer.SubmitAnswerCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "ans", null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_ForeignAttempt_ReturnsForbidden()
    {
        var attempt = new TestAttempt(Guid.NewGuid(), Guid.NewGuid());
        _attemptRepo.GetByIdWithAnswersAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);

        var result = await _handler.Handle(
            new AppSubmitAnswer.SubmitAnswerCommand(attempt.Id, Guid.NewGuid(), Guid.NewGuid(), "ans", null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task Handle_FinishedAttempt_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var attempt = new TestAttempt(Guid.NewGuid(), userId);
        InjectAnswers(attempt, [new AttemptAnswer(attempt.Id, Guid.NewGuid(), "a", true)]);
        attempt.Finish(Domain.ValueObjects.TestScore.From(1, 1));

        _attemptRepo.GetByIdWithAnswersAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);

        var result = await _handler.Handle(
            new AppSubmitAnswer.SubmitAnswerCommand(attempt.Id, Guid.NewGuid(), userId, "ans", null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Failure, result.Status);
    }

    [Fact]
    public async Task Handle_DuplicateQuestion_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var testId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var attempt = new TestAttempt(testId, userId);
        InjectAnswers(attempt, [new AttemptAnswer(attempt.Id, questionId, "first answer", true)]);

        _attemptRepo.GetByIdWithAnswersAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);

        var result = await _handler.Handle(
            new AppSubmitAnswer.SubmitAnswerCommand(attempt.Id, questionId, userId, "second answer", null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Failure, result.Status);
    }

    [Fact]
    public async Task Handle_QuestionNotFound_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var attempt = new TestAttempt(Guid.NewGuid(), userId);

        _attemptRepo.GetByIdWithAnswersAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        _questionRepo.GetByIdWithOptionsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Question?)null);

        var result = await _handler.Handle(
            new AppSubmitAnswer.SubmitAnswerCommand(attempt.Id, Guid.NewGuid(), userId, "ans", null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_QuestionFromDifferentTest_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var attempt = new TestAttempt(Guid.NewGuid(), userId);
        var question = new Question(
            Guid.NewGuid(), null, Question.QuestionTypes.OpenAnswer,
            "Some Q", "answer", 0, "hash"); // different testId

        _attemptRepo.GetByIdWithAnswersAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        _questionRepo.GetByIdWithOptionsAsync(question.Id, Arg.Any<CancellationToken>()).Returns(question);

        var result = await _handler.Handle(
            new AppSubmitAnswer.SubmitAnswerCommand(attempt.Id, question.Id, userId, "answer", null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Failure, result.Status);
    }

    // ---- MultiSelectTheme ----

    private static void InjectOptions(Question question, IEnumerable<QuestionOption> options)
    {
        var field = typeof(Question).GetField("_options", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var list = (List<QuestionOption>)field.GetValue(question)!;
        list.AddRange(options);
    }

    private static void InjectAnswers(TestAttempt attempt, IEnumerable<AttemptAnswer> answers)
    {
        var field = typeof(TestAttempt).GetField("_answers", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var list = (List<AttemptAnswer>)field.GetValue(attempt)!;
        list.AddRange(answers);
    }

    [Fact]
    public async Task Handle_MultiSelectTheme_CorrectItems_ReturnsCorrect()
    {
        var userId = Guid.NewGuid();
        var testId = Guid.NewGuid();
        var attempt = new TestAttempt(testId, userId);
        var question = new Question(
            testId, null, Question.QuestionTypes.MultiSelectTheme,
            "Pick animals", "cat,dog", 0, "hash");

        InjectOptions(question, [
            new QuestionOption(question.Id, "cat", true, 0),
            new QuestionOption(question.Id, "dog", true, 1),
            new QuestionOption(question.Id, "chair", false, 2),
        ]);

        _attemptRepo.GetByIdWithAnswersAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        _questionRepo.GetByIdWithOptionsAsync(question.Id, Arg.Any<CancellationToken>()).Returns(question);

        var result = await _handler.Handle(
            new AppSubmitAnswer.SubmitAnswerCommand(attempt.Id, question.Id, userId, "cat,dog", null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsCorrect);
    }

    [Fact]
    public async Task Handle_MultiSelectTheme_DifferentOrder_ReturnsCorrect()
    {
        var userId = Guid.NewGuid();
        var testId = Guid.NewGuid();
        var attempt = new TestAttempt(testId, userId);
        var question = new Question(
            testId, null, Question.QuestionTypes.MultiSelectTheme,
            "Pick animals", "cat,dog", 0, "hash");

        InjectOptions(question, [
            new QuestionOption(question.Id, "cat", true, 0),
            new QuestionOption(question.Id, "dog", true, 1),
        ]);

        _attemptRepo.GetByIdWithAnswersAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        _questionRepo.GetByIdWithOptionsAsync(question.Id, Arg.Any<CancellationToken>()).Returns(question);

        var result = await _handler.Handle(
            new AppSubmitAnswer.SubmitAnswerCommand(attempt.Id, question.Id, userId, "dog,cat", null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsCorrect);
    }

    [Fact]
    public async Task Handle_MultiSelectTheme_WrongItems_ReturnsIncorrect()
    {
        var userId = Guid.NewGuid();
        var testId = Guid.NewGuid();
        var attempt = new TestAttempt(testId, userId);
        var question = new Question(
            testId, null, Question.QuestionTypes.MultiSelectTheme,
            "Pick animals", "cat,dog", 0, "hash");

        InjectOptions(question, [
            new QuestionOption(question.Id, "cat", true, 0),
            new QuestionOption(question.Id, "dog", true, 1),
            new QuestionOption(question.Id, "bird", false, 2),
        ]);

        _attemptRepo.GetByIdWithAnswersAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        _questionRepo.GetByIdWithOptionsAsync(question.Id, Arg.Any<CancellationToken>()).Returns(question);

        var result = await _handler.Handle(
            new AppSubmitAnswer.SubmitAnswerCommand(attempt.Id, question.Id, userId, "cat,bird", null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsCorrect);
    }

    // ---- SingleChoice ----

    [Fact]
    public async Task Handle_SingleChoice_ExactMatch_ReturnsCorrect()
    {
        var userId = Guid.NewGuid();
        var testId = Guid.NewGuid();
        var attempt = new TestAttempt(testId, userId);
        var question = new Question(
            testId, null, Question.QuestionTypes.TranslateToNative,
            "Translate apple", "яблуко", 0, "hash");

        InjectOptions(question, [
            new QuestionOption(question.Id, "яблуко", true, 0),
            new QuestionOption(question.Id, "апельсин", false, 1),
        ]);

        _attemptRepo.GetByIdWithAnswersAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        _questionRepo.GetByIdWithOptionsAsync(question.Id, Arg.Any<CancellationToken>()).Returns(question);

        var result = await _handler.Handle(
            new AppSubmitAnswer.SubmitAnswerCommand(attempt.Id, question.Id, userId, "яблуко", null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsCorrect);
    }

    [Fact]
    public async Task Handle_SingleChoice_CaseInsensitive_ReturnsCorrect()
    {
        var userId = Guid.NewGuid();
        var testId = Guid.NewGuid();
        var attempt = new TestAttempt(testId, userId);
        var question = new Question(
            testId, null, Question.QuestionTypes.TranslateToForeign,
            "Translate яблуко", "Apple", 0, "hash");

        InjectOptions(question, [
            new QuestionOption(question.Id, "Apple", true, 0),
            new QuestionOption(question.Id, "Orange", false, 1),
        ]);

        _attemptRepo.GetByIdWithAnswersAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        _questionRepo.GetByIdWithOptionsAsync(question.Id, Arg.Any<CancellationToken>()).Returns(question);

        var result = await _handler.Handle(
            new AppSubmitAnswer.SubmitAnswerCommand(attempt.Id, question.Id, userId, "apple", null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsCorrect);
    }

    [Fact]
    public async Task Handle_SingleChoice_WrongOption_ReturnsIncorrect()
    {
        var userId = Guid.NewGuid();
        var testId = Guid.NewGuid();
        var attempt = new TestAttempt(testId, userId);
        var question = new Question(
            testId, null, Question.QuestionTypes.TranslateToNative,
            "Translate apple", "яблуко", 0, "hash");

        InjectOptions(question, [
            new QuestionOption(question.Id, "яблуко", true, 0),
            new QuestionOption(question.Id, "апельсин", false, 1),
        ]);

        _attemptRepo.GetByIdWithAnswersAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        _questionRepo.GetByIdWithOptionsAsync(question.Id, Arg.Any<CancellationToken>()).Returns(question);

        var result = await _handler.Handle(
            new AppSubmitAnswer.SubmitAnswerCommand(attempt.Id, question.Id, userId, "апельсин", null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsCorrect);
    }
}
