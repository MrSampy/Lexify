using Lexify.Domain.Common;

namespace Lexify.Domain.Entities;

/// <summary>
/// A "Talk to Lexi" practice session: a chat, in the language being learned, whose purpose is to make
/// the user *produce* the words that are due for review (or that they keep forgetting) in real context —
/// the one modality flash-card review and generated tests don't cover. The target words are snapshotted
/// at start (<see cref="TargetWordIds"/>); when the conversation ends they are analysed for usage and fed
/// back into SM-2, closing the loop with the rest of the app.
/// </summary>
public sealed class Conversation : BaseEntity
{
    private readonly List<ConversationMessage> _messages = [];
    private readonly List<Guid> _targetWordIds = [];

    public Guid UserId { get; private set; }
    public short LanguageId { get; private set; }
    public string Title { get; private set; } = default!;

    /// <summary>Optional roleplay scenario/topic the conversation is framed around; null = free chat.</summary>
    public string? Scenario { get; private set; }

    public string Status { get; private set; } = default!;

    /// <summary>Ids of the words this session is meant to practise, snapshotted at start.</summary>
    public IReadOnlyList<Guid> TargetWordIds => _targetWordIds.AsReadOnly();

    public DateTimeOffset? EndedAt { get; private set; }

    /// <summary>Final challenge score, recorded when the conversation ends. Null for sessions ended before scoring existed.</summary>
    public int? Points { get; private set; }
    public int? Stars { get; private set; }
    public int? WordsUsed { get; private set; }

    public IReadOnlyCollection<ConversationMessage> Messages => _messages.AsReadOnly();

    public bool IsActive => Status == Statuses.Active;

    private Conversation() { }

    private Conversation(Guid userId, short languageId, string title, string? scenario, IEnumerable<Guid> targetWordIds)
    {
        UserId = userId;
        LanguageId = languageId;
        Title = title;
        Scenario = scenario;
        Status = Statuses.Active;
        _targetWordIds.AddRange(targetWordIds);
    }

    public static Conversation Create(
        Guid userId, short languageId, string title, string? scenario, IEnumerable<Guid> targetWordIds)
    {
        if (userId == Guid.Empty) throw new DomainException("User ID cannot be empty.");
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Conversation title cannot be empty.");
        return new Conversation(userId, languageId, title, scenario, targetWordIds);
    }

    public ConversationMessage AddMessage(string role, string content)
    {
        if (Status != Statuses.Active)
            throw new DomainException("Cannot add a message to a conversation that is not active.");

        var message = new ConversationMessage(Id, role, content, _messages.Count);
        _messages.Add(message);
        UpdatedAt = DateTimeOffset.UtcNow;
        return message;
    }

    public void End()
    {
        if (Status != Statuses.Active)
            throw new DomainException("Only an active conversation can be ended.");
        Status = Statuses.Ended;
        EndedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RecordScore(int points, int stars, int wordsUsed)
    {
        if (Status != Statuses.Ended)
            throw new DomainException("Score can only be recorded on an ended conversation.");
        Points = points;
        Stars = stars;
        WordsUsed = wordsUsed;
    }

    public static class Statuses
    {
        public const string Active = "active";
        public const string Ended = "ended";
    }

    public static class Roles
    {
        public const string User = "user";
        public const string Assistant = "assistant";

        public static readonly IReadOnlySet<string> All = new HashSet<string> { User, Assistant };
    }
}
