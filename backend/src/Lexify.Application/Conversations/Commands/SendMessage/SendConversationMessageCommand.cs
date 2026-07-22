using MediatR;

namespace Lexify.Application.Conversations.Commands.SendMessage;

/// <param name="NativeLanguage">The learner's language (UI locale), used only to frame gentle corrections.</param>
public sealed record SendConversationMessageCommand(
    Guid ConversationId,
    Guid UserId,
    string Message,
    string NativeLanguage)
    : IStreamRequest<ChatSseEvent>;
