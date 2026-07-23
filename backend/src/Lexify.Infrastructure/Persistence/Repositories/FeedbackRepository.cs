using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lexify.Infrastructure.Persistence.Repositories;

public sealed class FeedbackRepository(AppDbContext context) : IFeedbackRepository
{
    public Task<Feedback?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        context.Feedback.FirstOrDefaultAsync(f => f.Id == id, ct);

    public Task<Feedback?> GetByIdWithAttachmentsAsync(Guid id, CancellationToken ct = default) =>
        context.Feedback
            .Include(f => f.Attachments)
            .FirstOrDefaultAsync(f => f.Id == id, ct);

    public Task<string?> GetAuthorEmailAsync(Guid feedbackId, CancellationToken ct = default) =>
        context.Feedback
            .AsNoTracking()
            .Where(f => f.Id == feedbackId)
            .Select(f => context.Users.Where(u => u.Id == f.UserId).Select(u => u.Email).FirstOrDefault())
            .FirstOrDefaultAsync(ct);

    public async Task<(int Total, IReadOnlyList<FeedbackListRow> Items)> GetPagedAsync(
        string? type, string? status, string? category, string? search,
        DateTimeOffset? dateFrom, DateTimeOffset? dateTo,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.Feedback.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(f => f.Type == type);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(f => f.Status == status);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(f => f.Category == category);

        if (dateFrom.HasValue)
            query = query.Where(f => f.CreatedAt >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(f => f.CreatedAt <= dateTo.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            // A bare number is how an admin pastes a ticket code — match it exactly rather than
            // making them guess that the subject search would not find it.
            if (int.TryParse(term.TrimStart('L', 'X', 'l', 'x', '-'), out var ticketNumber))
            {
                query = query.Where(f => f.TicketNumber == ticketNumber);
            }
            else
            {
                var pattern = $"%{term}%";
                query = query.Where(f =>
                    EF.Functions.ILike(f.Subject, pattern) || EF.Functions.ILike(f.Message, pattern));
            }
        }

        return await PageAsync(query, page, pageSize, ct);
    }

    public async Task<(int Total, IReadOnlyList<FeedbackListRow> Items)> GetByUserIdAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default) =>
        await PageAsync(
            context.Feedback.AsNoTracking().Where(f => f.UserId == userId), page, pageSize, ct);

    public async Task AddAsync(Feedback feedback, CancellationToken ct = default) =>
        await context.Feedback.AddAsync(feedback, ct);

    private async Task<(int Total, IReadOnlyList<FeedbackListRow> Items)> PageAsync(
        IQueryable<Feedback> query, int page, int pageSize, CancellationToken ct)
    {
        var total = await query.CountAsync(ct);

        // Left join on the author: the account may have been deleted (user_id is SET NULL), and the
        // submission still matters.
        var items = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new FeedbackListRow(
                f.Id,
                f.TicketNumber,
                f.UserId,
                context.Users.Where(u => u.Id == f.UserId).Select(u => u.Email).FirstOrDefault(),
                f.Type,
                f.Category,
                f.Subject,
                f.Rating,
                f.Status,
                f.CreatedAt,
                f.Attachments.Count))
            .ToListAsync(ct);

        return (total, items);
    }
}
