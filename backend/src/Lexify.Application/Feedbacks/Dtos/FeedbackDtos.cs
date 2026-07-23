namespace Lexify.Application.Feedbacks.Dtos;

/// <summary>What the submitter gets back — enough to quote the ticket when following up.</summary>
public sealed record SubmitFeedbackResultDto(Guid Id, int TicketNumber, string TicketCode);

public sealed record FeedbackAttachmentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes);

/// <summary>One row in the admin triage list or the user's own history.</summary>
public sealed record FeedbackListItemDto(
    Guid Id,
    int TicketNumber,
    string TicketCode,
    Guid? UserId,
    string? UserEmail,
    string Type,
    string? Category,
    string Subject,
    short? Rating,
    string Status,
    DateTimeOffset CreatedAt,
    int AttachmentCount);

/// <summary>Full submission, admin view — includes the internal note.</summary>
public sealed record FeedbackDetailDto(
    Guid Id,
    int TicketNumber,
    string TicketCode,
    Guid? UserId,
    string? UserEmail,
    string Type,
    string? Category,
    string Subject,
    string Message,
    short? Rating,
    string? ContactEmail,
    string Status,
    string? AdminNote,
    Guid? ResolvedBy,
    DateTimeOffset? ResolvedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<FeedbackAttachmentDto> Attachments);

/// <summary>An attachment's bytes on their way to <c>File(...)</c>.</summary>
public sealed record FeedbackAttachmentFileDto(byte[] Content, string ContentType, string FileName);
