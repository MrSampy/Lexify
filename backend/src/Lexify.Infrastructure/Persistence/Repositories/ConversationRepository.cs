using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lexify.Infrastructure.Persistence.Repositories;

public sealed class ConversationRepository(AppDbContext context) : IConversationRepository
{
    public Task<Conversation?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        context.Conversations.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Conversation?> GetByIdWithMessagesAsync(Guid id, CancellationToken ct = default) =>
        context.Conversations
            .Include(c => c.Messages.OrderBy(m => m.SortOrder))
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Conversation>> GetByUserIdAsync(
        Guid userId, int skip = 0, int take = 20, CancellationToken ct = default) =>
        await context.Conversations
            .Include(c => c.Messages)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public Task<int> CountByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        context.Conversations.CountAsync(c => c.UserId == userId, ct);

    public async Task AddAsync(Conversation conversation, CancellationToken ct = default) =>
        await context.Conversations.AddAsync(conversation, ct);

    public Task UpdateAsync(Conversation conversation, CancellationToken ct = default)
    {
        context.Conversations.Update(conversation);
        return Task.CompletedTask;
    }

    public async Task<bool> TryEndAsync(Guid id, CancellationToken ct = default)
    {
        // UpdatedAt is deliberately not set here (EF rejects value-generated properties in
        // ExecuteUpdate); the caller syncs the tracked aggregate via End(), which refreshes it.
        var now = DateTimeOffset.UtcNow;
        var updated = await context.Conversations
            .Where(c => c.Id == id && c.Status == Conversation.Statuses.Active)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.Status, Conversation.Statuses.Ended)
                .SetProperty(c => c.EndedAt, now), ct);
        return updated == 1;
    }
}
