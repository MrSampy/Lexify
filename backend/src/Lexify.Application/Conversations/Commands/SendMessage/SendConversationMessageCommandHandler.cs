using System.Runtime.CompilerServices;
using System.Text;
using Lexify.Application.Abstractions;
using Lexify.Application.AI.Dtos;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Conversations.Commands.SendMessage;

public sealed class SendConversationMessageCommandHandler(
    IConversationRepository conversationRepository,
    IWordRepository wordRepository,
    ILanguageRepository languageRepository,
    IUserRepository userRepository,
    IAiQuotaService aiQuota,
    IAIProvider aiProvider,
    IUnitOfWork unitOfWork)
    : IStreamRequestHandler<SendConversationMessageCommand, ChatSseEvent>
{
    private const int MaxMessageLength = 2000;
    // Same cap as StartConversationCommandValidator — this field is interpolated into the LLM system
    // prompt, so it must not be an unbounded free-text channel.
    private const int MaxNativeLanguageLength = 50;
    // Hard ceiling on transcript length. The client stops at the message budget (≤ ~17 rows for the
    // largest budget); this only guards against clients that bypass the budget to farm LLM calls.
    private const int MaxMessagesPerConversation = 40;
    // Only the most recent turns are sent to the LLM — token cost stays flat as a chat grows, and a
    // 16-turn window is far more context than a short practice reply needs. End-of-conversation
    // analysis still sees the full history.
    private const int HistoryWindowTurns = 16;

    public async IAsyncEnumerable<ChatSseEvent> Handle(
        SendConversationMessageCommand request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // IStreamRequest bypasses the pipeline behaviors (validation, quota, transaction), so everything
        // has to be enforced here — mirrors FormatWordsCommandHandler.
        var message = request.Message?.Trim() ?? string.Empty;
        if (message.Length == 0)
        {
            yield return new ChatSseEvent("error", ErrorMessage: "Message cannot be empty.");
            yield break;
        }
        if (message.Length > MaxMessageLength)
        {
            yield return new ChatSseEvent("error", ErrorMessage: $"Message is too long (max {MaxMessageLength} characters).");
            yield break;
        }

        var nativeLanguage = request.NativeLanguage?.Trim() ?? string.Empty;
        if (nativeLanguage.Length is 0 or > MaxNativeLanguageLength)
        {
            yield return new ChatSseEvent("error", ErrorMessage: "Native language is missing or too long.");
            yield break;
        }

        var quota = await aiQuota.CheckAsync(request.UserId, cancellationToken);
        if (quota.IsExceeded)
        {
            yield return new ChatSseEvent("error", ErrorMessage:
                $"Daily AI limit reached ({quota.Used}/{quota.Limit}). It resets at midnight UTC.");
            yield break;
        }

        var conversation = await conversationRepository.GetByIdWithMessagesAsync(request.ConversationId, cancellationToken);
        if (conversation is null || conversation.UserId != request.UserId)
        {
            yield return new ChatSseEvent("error", ErrorMessage: "Conversation not found.");
            yield break;
        }
        if (!conversation.IsActive)
        {
            yield return new ChatSseEvent("error", ErrorMessage: "This conversation has already ended.");
            yield break;
        }
        if (conversation.Messages.Count >= MaxMessagesPerConversation)
        {
            yield return new ChatSseEvent("error", ErrorMessage:
                "This conversation has reached its message limit. Finish it to see your score.");
            yield break;
        }

        var context = await BuildContextAsync(conversation, nativeLanguage, cancellationToken);

        // Persist the user's turn before streaming the reply, so a dropped connection mid-reply doesn't
        // lose what the learner said.
        // TrySave: absorbs the unique-index violation two concurrent sends can race into on
        // (conversation_id, sort_order) — the loser reports a retry-able error instead of a 500.
        conversation.AddMessage(Conversation.Roles.User, message);
        if (!await unitOfWork.TrySaveChangesAsync(cancellationToken))
        {
            yield return new ChatSseEvent("error", ErrorMessage: "Couldn't save your message — please try again.");
            yield break;
        }

        var window = conversation.Messages
            .OrderBy(m => m.SortOrder)
            .Select(m => new ChatTurn(m.Role, m.Content))
            .TakeLast(HistoryWindowTurns)
            .ToList();

        var replyBuilder = new StringBuilder();
        await foreach (var chunk in aiProvider.StreamChatReplyAsync(context, window, cancellationToken))
        {
            replyBuilder.Append(chunk);
            yield return new ChatSseEvent("streaming", Chunk: chunk);
        }

        var reply = replyBuilder.ToString().Trim();
        if (reply.Length == 0)
        {
            yield return new ChatSseEvent("error", ErrorMessage: "Lexi is unavailable right now. Please try again.");
            yield break;
        }

        conversation.AddMessage(Conversation.Roles.Assistant, reply);
        if (!await unitOfWork.TrySaveChangesAsync(cancellationToken))
        {
            yield return new ChatSseEvent("error", ErrorMessage: "Couldn't save Lexi's reply — please try again.");
            yield break;
        }

        yield return new ChatSseEvent("done");
    }

    private async Task<ChatContext> BuildContextAsync(
        Conversation conversation, string nativeLanguage, CancellationToken ct)
    {
        var targets = new List<TargetWord>();
        foreach (var wordId in conversation.TargetWordIds)
        {
            var word = await wordRepository.GetByIdAsync(wordId, ct);
            if (word is not null)
                targets.Add(new TargetWord(word.Id, word.Term, word.Translation));
        }

        var language = await languageRepository.GetByIdAsync(conversation.LanguageId, ct);
        var user = await userRepository.GetByIdAsync(conversation.UserId, ct);

        return new ChatContext(
            TargetLanguage: language?.Name ?? "the target language",
            NativeLanguage: nativeLanguage,
            CefrLevel: user?.EnglishLevel, // same signal Start uses — mid-chat replies must keep the level
            Scenario: conversation.Scenario,
            TargetWords: targets);
    }
}
