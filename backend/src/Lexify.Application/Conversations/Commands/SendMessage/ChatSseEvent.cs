namespace Lexify.Application.Conversations.Commands.SendMessage;

public sealed record ChatSseEvent(
    string EventType,
    string? Chunk = null,
    string? ErrorMessage = null);
