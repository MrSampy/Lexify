namespace Lexify.Domain.Entities;

/// <summary>
/// One turn in a <see cref="Conversation"/> — either the learner ("user") or Lexi ("assistant"). The
/// system prompt is not stored as a message: it is rebuilt on the fly from the conversation's context so
/// prompt tweaks apply retroactively and no prompt text leaks into transcripts.
/// </summary>
public sealed class ConversationMessage
{
    public Guid Id { get; private set; }
    public Guid ConversationId { get; private set; }
    public string Role { get; private set; } = default!;
    public string Content { get; private set; } = default!;
    public int SortOrder { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private ConversationMessage() { }

    public ConversationMessage(Guid conversationId, string role, string content, int sortOrder)
    {
        Id = Guid.NewGuid();
        ConversationId = conversationId;
        Role = role;
        Content = content;
        SortOrder = sortOrder;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
