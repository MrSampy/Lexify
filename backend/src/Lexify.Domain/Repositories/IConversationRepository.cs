using Lexify.Domain.Entities;

namespace Lexify.Domain.Repositories;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>The conversation with all its messages loaded, ordered by <c>SortOrder</c>.</summary>
    Task<Conversation?> GetByIdWithMessagesAsync(Guid id, CancellationToken ct = default);

    /// <summary>The user's conversations, newest first (messages not loaded).</summary>
    Task<IReadOnlyList<Conversation>> GetByUserIdAsync(
        Guid userId, int skip = 0, int take = 20, CancellationToken ct = default);

    Task<int> CountByUserIdAsync(Guid userId, CancellationToken ct = default);

    Task AddAsync(Conversation conversation, CancellationToken ct = default);
    Task UpdateAsync(Conversation conversation, CancellationToken ct = default);
}
