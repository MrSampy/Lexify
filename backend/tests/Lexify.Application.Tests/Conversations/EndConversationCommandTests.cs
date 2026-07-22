using Lexify.Application.AI.Dtos;
using Lexify.Application.Abstractions;
using Lexify.Application.Conversations.Commands.EndConversation;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using NSubstitute;

namespace Lexify.Application.Tests.Conversations;

public class EndConversationCommandTests
{
    private readonly IConversationRepository _conversationRepo = Substitute.For<IConversationRepository>();
    private readonly IWordRepository _wordRepo = Substitute.For<IWordRepository>();
    private readonly ILanguageRepository _languageRepo = Substitute.For<ILanguageRepository>();
    private readonly IReviewLogRepository _reviewLogRepo = Substitute.For<IReviewLogRepository>();
    private readonly IAIProvider _ai = Substitute.For<IAIProvider>();
    private readonly EndConversationCommandHandler _handler;

    public EndConversationCommandTests()
    {
        _handler = new EndConversationCommandHandler(
            _conversationRepo, _wordRepo, _languageRepo, _reviewLogRepo, _ai);
    }

    [Fact]
    public async Task Handle_MapsVerdictsToSm2_AndSkipsUnusedWords()
    {
        var userId = Guid.NewGuid();
        var blockId = Guid.NewGuid();

        var usedCorrect = new Word(blockId, "embark", "вирушати");
        var usedWrong = new Word(blockId, "unwind", "розслаблятися");
        var notUsed = new Word(blockId, "surge", "сплеск");

        var conversation = Conversation.Create(
            userId, 3, "Chat", null,
            [usedCorrect.Id, usedWrong.Id, notUsed.Id]);
        conversation.AddMessage(Conversation.Roles.Assistant, "Hello!");
        conversation.AddMessage(Conversation.Roles.User, "I embark on a trip.");

        _conversationRepo.GetByIdWithMessagesAsync(conversation.Id, Arg.Any<CancellationToken>())
            .Returns(conversation);
        _wordRepo.GetByIdAsync(usedCorrect.Id, Arg.Any<CancellationToken>()).Returns(usedCorrect);
        _wordRepo.GetByIdAsync(usedWrong.Id, Arg.Any<CancellationToken>()).Returns(usedWrong);
        _wordRepo.GetByIdAsync(notUsed.Id, Arg.Any<CancellationToken>()).Returns(notUsed);

        _ai.AnalyzeConversationAsync(
                Arg.Any<IReadOnlyList<ChatTurn>>(),
                Arg.Any<IReadOnlyList<TargetWord>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<WordUsageVerdict>
            {
                new(usedCorrect.Id, Used: true, UsedCorrectly: true, Note: "Great!"),
                new(usedWrong.Id, Used: true, UsedCorrectly: false, Note: "Almost"),
                new(notUsed.Id, Used: false, UsedCorrectly: false, Note: null),
            });

        var result = await _handler.Handle(
            new EndConversationCommand(conversation.Id, userId), CancellationToken.None);

        Assert.True(result.IsSuccess);

        // used+correct → recall (quality 4): repetitions advance, no lapse
        Assert.Equal(1, usedCorrect.Repetitions);
        Assert.Equal(0, usedCorrect.LapseCount);
        Assert.NotNull(usedCorrect.LastReviewedAt);

        // used+wrong → lapse (quality 2 < recall threshold)
        Assert.Equal(0, usedWrong.Repetitions);
        Assert.Equal(1, usedWrong.LapseCount);

        // not used → left completely untouched
        Assert.Null(notUsed.LastReviewedAt);

        // Only the two used words are rated and logged; the unused one is skipped.
        await _wordRepo.Received(1).UpdateAsync(usedCorrect, Arg.Any<CancellationToken>());
        await _wordRepo.Received(1).UpdateAsync(usedWrong, Arg.Any<CancellationToken>());
        await _wordRepo.DidNotReceive().UpdateAsync(notUsed, Arg.Any<CancellationToken>());
        await _reviewLogRepo.Received(2).AddAsync(
            Arg.Is<WordReviewLog>(l => l.Source == WordReviewLog.Sources.Conversation),
            Arg.Any<CancellationToken>());

        // Summary reflects usage; the conversation is ended.
        Assert.Equal(3, result.Value!.Words.Count);
        Assert.Contains(result.Value.Words, w => w.WordId == usedCorrect.Id && w.Used && w.UsedCorrectly && w.IntervalDays != null);
        Assert.Contains(result.Value.Words, w => w.WordId == notUsed.Id && !w.Used && w.IntervalDays == null);
        Assert.Equal(Conversation.Statuses.Ended, conversation.Status);
    }

    [Fact]
    public async Task Handle_DetectsUsedWord_EvenWhenLlmVerdictEmpty()
    {
        // Regression: the "I used it but it said I didn't" bug — usage is decided deterministically
        // from the learner's turns, not from the (unreliable) LLM verdict.
        var userId = Guid.NewGuid();
        var blockId = Guid.NewGuid();
        var word = new Word(blockId, "embark", "вирушати");

        var conversation = Conversation.Create(userId, 3, "Chat", null, [word.Id]);
        conversation.AddMessage(Conversation.Roles.Assistant, "Hi!");
        conversation.AddMessage(Conversation.Roles.User, "Today I will EMBARK, on a journey!");

        _conversationRepo.GetByIdWithMessagesAsync(conversation.Id, Arg.Any<CancellationToken>())
            .Returns(conversation);
        _wordRepo.GetByIdAsync(word.Id, Arg.Any<CancellationToken>()).Returns(word);

        // LLM says nothing (unavailable / missed the word).
        _ai.AnalyzeConversationAsync(
                Arg.Any<IReadOnlyList<ChatTurn>>(), Arg.Any<IReadOnlyList<TargetWord>>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<WordUsageVerdict>());

        var result = await _handler.Handle(
            new EndConversationCommand(conversation.Id, userId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var w = Assert.Single(result.Value!.Words);
        Assert.True(w.Used);            // detected despite empty verdict
        Assert.True(w.UsedCorrectly);   // generous default → SM-2 quality 4
        Assert.Equal(1, word.Repetitions);
        await _reviewLogRepo.Received(1).AddAsync(Arg.Any<WordReviewLog>(), Arg.Any<CancellationToken>());
        Assert.True(result.Value.Score.WordsUsed >= 1);
        Assert.True(result.Value.Score.Points >= 10);
    }

    [Fact]
    public async Task Handle_ForeignUser_ReturnsNotFound()
    {
        var conversation = Conversation.Create(Guid.NewGuid(), 1, "Chat", null, []);
        _conversationRepo.GetByIdWithMessagesAsync(conversation.Id, Arg.Any<CancellationToken>())
            .Returns(conversation);

        var result = await _handler.Handle(
            new EndConversationCommand(conversation.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }
}
