using Lexify.Application.AI.Dtos;
using Lexify.Application.Abstractions;
using Lexify.Application.Conversations.Commands.SendMessage;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using NSubstitute;

namespace Lexify.Application.Tests.Conversations;

public class SendConversationMessageCommandTests
{
    private readonly IConversationRepository _conversationRepo = Substitute.For<IConversationRepository>();
    private readonly IWordRepository _wordRepo = Substitute.For<IWordRepository>();
    private readonly ILanguageRepository _languageRepo = Substitute.For<ILanguageRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IAiQuotaService _quota = Substitute.For<IAiQuotaService>();
    private readonly IAIProvider _ai = Substitute.For<IAIProvider>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly SendConversationMessageCommandHandler _handler;

    public SendConversationMessageCommandTests()
    {
        _quota.CheckAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(AiQuotaCheck.Unlimited);
        _unitOfWork.TrySaveChangesAsync(Arg.Any<CancellationToken>()).Returns(true);
        _handler = new SendConversationMessageCommandHandler(
            _conversationRepo, _wordRepo, _languageRepo, _userRepo, _quota, _ai, _unitOfWork);
    }

    private Conversation SetupConversation(Guid userId, int messageCount)
    {
        var conversation = Conversation.Create(userId, 3, "Chat", null, []);
        for (var i = 0; i < messageCount; i++)
        {
            conversation.AddMessage(
                i % 2 == 0 ? Conversation.Roles.Assistant : Conversation.Roles.User, $"turn {i}");
        }
        _conversationRepo.GetByIdWithMessagesAsync(conversation.Id, Arg.Any<CancellationToken>())
            .Returns(conversation);
        return conversation;
    }

    private static async IAsyncEnumerable<string> OneChunk(string chunk)
    {
        await Task.Yield();
        yield return chunk;
    }

    [Fact]
    public async Task Handle_LongTranscript_SendsOnlyTheLast16TurnsToTheLlm()
    {
        var userId = Guid.NewGuid();
        var conversation = SetupConversation(userId, messageCount: 21);

        IReadOnlyList<ChatTurn>? captured = null;
        _ai.StreamChatReplyAsync(
                Arg.Any<ChatContext>(),
                Arg.Do<IReadOnlyList<ChatTurn>>(h => captured = h),
                Arg.Any<CancellationToken>())
            .Returns(OneChunk("Nice!"));

        var events = new List<ChatSseEvent>();
        await foreach (var evt in _handler.Handle(
            new SendConversationMessageCommand(conversation.Id, userId, "Hello!", "English"),
            CancellationToken.None))
        {
            events.Add(evt);
        }

        Assert.Equal("done", events[^1].EventType);
        Assert.NotNull(captured);
        // 21 existing + the new user turn = 22, windowed down to the most recent 16.
        Assert.Equal(16, captured!.Count);
        Assert.Equal("Hello!", captured[^1].Content); // the fresh turn is inside the window
    }

    [Fact]
    public async Task Handle_AtMessageCap_YieldsErrorWithoutPersistingOrCallingLlm()
    {
        var userId = Guid.NewGuid();
        var conversation = SetupConversation(userId, messageCount: 40);

        var events = new List<ChatSseEvent>();
        await foreach (var evt in _handler.Handle(
            new SendConversationMessageCommand(conversation.Id, userId, "One more!", "English"),
            CancellationToken.None))
        {
            events.Add(evt);
        }

        var error = Assert.Single(events);
        Assert.Equal("error", error.EventType);
        Assert.Contains("message limit", error.ErrorMessage);
        Assert.Equal(40, conversation.Messages.Count); // nothing appended
        await _unitOfWork.DidNotReceive().TrySaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SaveConflict_YieldsRetryableErrorInsteadOf500()
    {
        var userId = Guid.NewGuid();
        var conversation = SetupConversation(userId, messageCount: 1);
        _unitOfWork.TrySaveChangesAsync(Arg.Any<CancellationToken>()).Returns(false);

        var events = new List<ChatSseEvent>();
        await foreach (var evt in _handler.Handle(
            new SendConversationMessageCommand(conversation.Id, userId, "Hello!", "English"),
            CancellationToken.None))
        {
            events.Add(evt);
        }

        var error = Assert.Single(events);
        Assert.Equal("error", error.EventType);
        Assert.Contains("try again", error.ErrorMessage);
        _ = _ai.DidNotReceive().StreamChatReplyAsync(
            Arg.Any<ChatContext>(), Arg.Any<IReadOnlyList<ChatTurn>>(), Arg.Any<CancellationToken>());
    }
}
