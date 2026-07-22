using Lexify.Domain.Entities;

namespace Lexify.Domain.Repositories;

/// <summary>List-view projection: everything a history row needs without loading message bodies.</summary>
public sealed record ConversationListRow(
    Guid Id,
    short LanguageId,
    string Title,
    string? Scenario,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? EndedAt,
    int MessageCount,
    int? Points,
    int? Stars);

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>The conversation with all its messages loaded, ordered by <c>SortOrder</c>.</summary>
    Task<Conversation?> GetByIdWithMessagesAsync(Guid id, CancellationToken ct = default);

    /// <summary>The user's conversations, newest first, as list rows (message COUNT only, no bodies).</summary>
    Task<IReadOnlyList<ConversationListRow>> GetListByUserIdAsync(
        Guid userId, int skip = 0, int take = 20, CancellationToken ct = default);

    Task<int> CountByUserIdAsync(Guid userId, CancellationToken ct = default);

    Task<int> CountEndedByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Average recorded stars over the user's ended conversations; null when none have a score.</summary>
    Task<double?> GetAverageStarsAsync(Guid userId, CancellationToken ct = default);

    Task AddAsync(Conversation conversation, CancellationToken ct = default);
    Task UpdateAsync(Conversation conversation, CancellationToken ct = default);

    /// <summary>
    /// Atomically flips the conversation from active to ended. Returns false when it was not active —
    /// the guard that makes ending idempotent under concurrent/duplicate requests, so SM-2 is applied
    /// exactly once per conversation.
    /// </summary>
    Task<bool> TryEndAsync(Guid id, CancellationToken ct = default);
}
