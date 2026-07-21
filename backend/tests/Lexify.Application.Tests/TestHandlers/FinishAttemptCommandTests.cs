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
    private readonly IWordBlockRepository _blockRepo;
    private readonly IReviewLogRepository _reviewLogRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AppFinishAttempt.FinishAttemptCommandHandler _handler;

    public FinishAttemptCommandTests()
    {
        _attemptRepo = Substitute.For<ITestAttemptRepository>();
        _questionRepo = Substitute.For<IQuestionRepository>();
        _wordRepo = Substitute.For<IWordRepository>();
        _blockRepo = Substitute.For<IWordBlockRepository>();
        _reviewLogRepo = Substitute.For<IReviewLogRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new AppFinishAttempt.FinishAttemptCommandHandler(
            _attemptRepo, _questionRepo, _wordRepo, _blockRepo, _reviewLogRepo, _unitOfWork);
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
    public async Task Handle_NullWordIdQuestion_FinishesWithoutTouchingSm2()
    {
        // matching_pairs spans several words and stores WordId = null — finishing an attempt with
        // such an answer must succeed and must not feed anything into spaced repetition.
        var userId = Guid.NewGuid();
        var testId = Guid.NewGuid();
        var attempt = new TestAttempt(testId, userId);
        var question = new Question(
            testId, null, Question.QuestionTypes.MatchingPairs,
            "Match each word to its translation: dog, cat.", "dog → собака; cat → кіт", 0, "hash");

        InjectAnswers(attempt, [new AttemptAnswer(attempt.Id, question.Id, "dog|собака;cat|кіт", true)]);

        _attemptRepo.GetByIdWithAnswersAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        _questionRepo.GetByTestIdAsync(testId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Question>)[question]);

        var result = await _handler.Handle(
            new AppFinishAttempt.FinishAttemptCommand(attempt.Id, userId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(attempt.FinishedAt);
        await _wordRepo.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
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

    [Fact]
    public async Task Handle_AttemptNotFound_ReturnsNotFound()
    {
        _attemptRepo.GetByIdWithAnswersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((TestAttempt?)null);

        var result = await _handler.Handle(
            new AppFinishAttempt.FinishAttemptCommand(Guid.NewGuid(), Guid.NewGuid()),
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
            new AppFinishAttempt.FinishAttemptCommand(attempt.Id, Guid.NewGuid()), // different userId
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task Handle_NoAnswers_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var attempt = new TestAttempt(Guid.NewGuid(), userId); // _answers is empty

        _attemptRepo.GetByIdWithAnswersAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);

        var result = await _handler.Handle(
            new AppFinishAttempt.FinishAttemptCommand(attempt.Id, userId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Failure, result.Status);
    }

    [Fact]
    public async Task Handle_WrongAnswer_AppliesSm2PenaltyToLinkedWord()
    {
        var userId = Guid.NewGuid();
        var testId = Guid.NewGuid();
        var attempt = new TestAttempt(testId, userId);
        var wordId = Guid.NewGuid();
        var word = new Word(Guid.NewGuid(), "apple", "яблуко");

        var question = new Question(
            testId, wordId, Question.QuestionTypes.OpenAnswer,
            "Translate apple", "яблуко", 0, "hash");

        // Inject a wrong answer pointing at the question's actual Id
        InjectAnswers(attempt, [new AttemptAnswer(attempt.Id, question.Id, "wrong", false)]);

        _attemptRepo.GetByIdWithAnswersAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        _questionRepo.GetByTestIdAsync(testId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Question>)[question]);
        _wordRepo.GetByIdAsync(wordId, Arg.Any<CancellationToken>()).Returns(word);

        var initialEaseFactor = word.EaseFactor;

        var result = await _handler.Handle(
            new AppFinishAttempt.FinishAttemptCommand(attempt.Id, userId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(initialEaseFactor, word.EaseFactor); // SM-2 penalty reduced ease factor
        Assert.Equal(0, word.Repetitions); // quality=0 resets repetitions
    }

    [Fact]
    public async Task Handle_CorrectAnswer_AdvancesLinkedWordAndWritesTestReviewLog()
    {
        var userId = Guid.NewGuid();
        var testId = Guid.NewGuid();
        var attempt = new TestAttempt(testId, userId);
        var blockId = Guid.NewGuid();
        var word = new Word(blockId, "apple", "яблуко");
        var block = new WordBlock(userId, 3, "Fruits");

        var question = new Question(
            testId, word.Id, Question.QuestionTypes.OpenAnswer,
            "Translate apple", "яблуко", 0, "hash");

        InjectAnswers(attempt, [new AttemptAnswer(attempt.Id, question.Id, "яблуко", true)]);

        _attemptRepo.GetByIdWithAnswersAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        _questionRepo.GetByTestIdAsync(testId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Question>)[question]);
        _wordRepo.GetByIdAsync(word.Id, Arg.Any<CancellationToken>()).Returns(word);
        _blockRepo.GetByIdAsync(blockId, Arg.Any<CancellationToken>()).Returns(block);

        var result = await _handler.Handle(
            new AppFinishAttempt.FinishAttemptCommand(attempt.Id, userId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        // A correct answer now advances the schedule (previously only wrong answers were applied).
        Assert.Equal(1, word.Repetitions);
        await _reviewLogRepo.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<WordReviewLog>>(logs =>
                logs.Any(l => l.WordId == word.Id && l.Quality == 4 && l.Source == WordReviewLog.Sources.Test)),
            Arg.Any<CancellationToken>());
    }
}
