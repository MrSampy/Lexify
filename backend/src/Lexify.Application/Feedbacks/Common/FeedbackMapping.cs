using Lexify.Application.Feedbacks.Dtos;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;

namespace Lexify.Application.Feedbacks.Common;

/// <summary>Hand-written projections — the list row already comes shaped from the repository.</summary>
public static class FeedbackMapping
{
    public static FeedbackListItemDto ToListItem(FeedbackListRow row) =>
        new(row.Id,
            row.TicketNumber,
            TicketCode.From(row.TicketNumber),
            row.UserId,
            row.UserEmail,
            row.Type,
            row.Category,
            row.Subject,
            row.Rating,
            row.Status,
            row.CreatedAt,
            row.AttachmentCount);

    public static FeedbackDetailDto ToDetail(Feedback feedback, string? userEmail) =>
        new(feedback.Id,
            feedback.TicketNumber,
            TicketCode.From(feedback.TicketNumber),
            feedback.UserId,
            userEmail,
            feedback.Type,
            feedback.Category,
            feedback.Subject,
            feedback.Message,
            feedback.Rating,
            feedback.ContactEmail,
            feedback.Status,
            feedback.AdminNote,
            feedback.ResolvedBy,
            feedback.ResolvedAt,
            feedback.CreatedAt,
            feedback.UpdatedAt,
            feedback.Attachments
                .OrderBy(a => a.CreatedAt)
                .Select(a => new FeedbackAttachmentDto(a.Id, a.FileName, a.ContentType, a.SizeBytes))
                .ToList());
}
