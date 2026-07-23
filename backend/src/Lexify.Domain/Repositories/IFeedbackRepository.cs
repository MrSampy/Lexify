using Lexify.Domain.Entities;

namespace Lexify.Domain.Repositories;

/// <summary>List-view projection: everything a triage row needs without loading the message body.</summary>
public sealed record FeedbackListRow(
    Guid Id,
    int TicketNumber,
    Guid? UserId,
    string? UserEmail,
    string Type,
    string? Category,
    string Subject,
    short? Rating,
    string Status,
    DateTimeOffset CreatedAt,
    int AttachmentCount);

public interface IFeedbackRepository
{
    Task<Feedback?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>The submission with its attachment metadata loaded.</summary>
    Task<Feedback?> GetByIdWithAttachmentsAsync(Guid id, CancellationToken ct = default);

    /// <summary>Email of the submission's author, or null when it was anonymised by account deletion.</summary>
    Task<string?> GetAuthorEmailAsync(Guid feedbackId, CancellationToken ct = default);

    /// <summary>Admin triage list: newest first, filtered, with the author's email joined in.</summary>
    Task<(int Total, IReadOnlyList<FeedbackListRow> Items)> GetPagedAsync(
        string? type, string? status, string? category, string? search,
        DateTimeOffset? dateFrom, DateTimeOffset? dateTo,
        int page, int pageSize, CancellationToken ct = default);

    /// <summary>The caller's own submissions, newest first.</summary>
    Task<(int Total, IReadOnlyList<FeedbackListRow> Items)> GetByUserIdAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default);

    Task AddAsync(Feedback feedback, CancellationToken ct = default);
}
