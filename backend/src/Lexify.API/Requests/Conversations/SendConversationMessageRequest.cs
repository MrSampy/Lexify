namespace Lexify.API.Requests.Conversations;

public sealed record SendConversationMessageRequest(
    string Message,
    string NativeLanguage);
