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
}
