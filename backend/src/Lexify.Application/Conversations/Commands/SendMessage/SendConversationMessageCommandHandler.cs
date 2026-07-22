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
    IAiQuotaService aiQuota,
    IAIProvider aiProvider,
    IUnitOfWork unitOfWork)
    : IStreamRequestHandler<SendConversationMessageCommand, ChatSseEvent>
{
    private const int MaxMessageLength = 2000;

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

        var context = await BuildContextAsync(conversation, request.NativeLanguage, cancellationToken);

        // Persist the user's turn before streaming the reply, so a dropped connection mid-reply doesn't
        // lose what the learner said.
        conversation.AddMessage(Conversation.Roles.User, message);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var history = conversation.Messages
            .OrderBy(m => m.SortOrder)
            .Select(m => new ChatTurn(m.Role, m.Content))
            .ToList();

        var replyBuilder = new StringBuilder();
        await foreach (var chunk in aiProvider.StreamChatReplyAsync(context, history, cancellationToken))
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
        await unitOfWork.SaveChangesAsync(cancellationToken);

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

        return new ChatContext(
            TargetLanguage: language?.Name ?? "the target language",
            NativeLanguage: nativeLanguage,
            CefrLevel: null,
            Scenario: conversation.Scenario,
            TargetWords: targets);
    }
}
