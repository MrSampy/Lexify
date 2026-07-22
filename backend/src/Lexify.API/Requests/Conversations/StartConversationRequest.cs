namespace Lexify.API.Requests.Conversations;

public sealed record StartConversationRequest(
    Guid? BlockId,
    string? Scenario,
    string NativeLanguage);
