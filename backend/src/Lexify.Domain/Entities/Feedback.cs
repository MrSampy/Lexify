using Lexify.Domain.Common;

namespace Lexify.Domain.Entities;

/// <summary>
/// A support/feedback submission: the one channel through which a learner can report a bug, propose an
/// idea, rate the app, or ask a question. Each row gets a short human-readable ticket number
/// (<see cref="TicketNumber"/>, shown as <c>LX-1042</c>) so the user can refer back to it, and moves
/// through a small triage lifecycle handled by admins.
/// </summary>
public sealed class Feedback : BaseEntity
{
    private readonly List<FeedbackAttachment> _attachments = [];

    /// <summary>DB-generated sequential number; the user-facing ticket code is <c>LX-{TicketNumber}</c>.</summary>
    public int TicketNumber { get; private set; }

    /// <summary>Author. Nullable so a submission survives the account being deleted (FK is SET NULL).</summary>
    public Guid? UserId { get; private set; }

    public string Type { get; private set; } = default!;

    /// <summary>Which part of the app the submission is about; null = unspecified.</summary>
    public string? Category { get; private set; }

    public string Subject { get; private set; } = default!;
    public string Message { get; private set; } = default!;

    /// <summary>1–5 stars. Required for <see cref="Types.Review"/>, null for every other type.</summary>
    public short? Rating { get; private set; }

    /// <summary>Where to reply; defaults to the account email but the user may override it.</summary>
    public string? ContactEmail { get; private set; }

    public string Status { get; private set; } = default!;

    /// <summary>Internal triage note — never shown to the submitter.</summary>
    public string? AdminNote { get; private set; }

    public Guid? ResolvedBy { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }

    public IReadOnlyCollection<FeedbackAttachment> Attachments => _attachments.AsReadOnly();

    private Feedback() { }

    private Feedback(
        Guid? userId, string type, string? category, string subject,
        string message, short? rating, string? contactEmail)
    {
        UserId = userId;
        Type = type;
        Category = category;
        Subject = subject;
        Message = message;
        Rating = rating;
        ContactEmail = contactEmail;
        Status = Statuses.New;
    }

    public static Feedback Create(
        Guid? userId, string type, string? category, string subject,
        string message, short? rating, string? contactEmail)
    {
        if (!Types.All.Contains(type))
            throw new DomainException($"Unknown feedback type '{type}'.");
        if (string.IsNullOrWhiteSpace(subject))
            throw new DomainException("Feedback subject cannot be empty.");
        if (string.IsNullOrWhiteSpace(message))
            throw new DomainException("Feedback message cannot be empty.");

        // A rating only means something on a review; carrying one on a bug report would silently
        // skew any "average rating" reporting built on this table later.
        if (type == Types.Review)
        {
            if (rating is not (>= 1 and <= 5))
                throw new DomainException("A review must carry a rating between 1 and 5.");
        }
        else if (rating is not null)
        {
            throw new DomainException("Only a review can carry a rating.");
        }

        return new Feedback(userId, type, category, subject.Trim(), message.Trim(), rating, contactEmail);
    }

    public FeedbackAttachment AddAttachment(
        string fileName, string contentType, long sizeBytes, string storageName)
    {
        var attachment = new FeedbackAttachment(Id, fileName, contentType, sizeBytes, storageName);
        _attachments.Add(attachment);
        return attachment;
    }

    /// <summary>
    /// Moves the ticket through triage. Any status may be re-entered or revisited — an admin who
    /// resolves a ticket by mistake must be able to put it back in progress.
    /// </summary>
    public void UpdateStatus(string status, string? adminNote, Guid adminId)
    {
        if (!Statuses.All.Contains(status))
            throw new DomainException($"Unknown feedback status '{status}'.");

        Status = status;
        AdminNote = string.IsNullOrWhiteSpace(adminNote) ? null : adminNote.Trim();

        if (status == Statuses.Resolved)
        {
            ResolvedBy = adminId;
            ResolvedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            ResolvedBy = null;
            ResolvedAt = null;
        }

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static class Types
    {
        public const string Suggestion = "suggestion";
        public const string Bug = "bug";
        public const string Review = "review";
        public const string Question = "question";

        public static readonly IReadOnlySet<string> All =
            new HashSet<string> { Suggestion, Bug, Review, Question };
    }

    public static class Statuses
    {
        public const string New = "new";
        public const string InProgress = "in_progress";
        public const string Resolved = "resolved";

        public static readonly IReadOnlySet<string> All =
            new HashSet<string> { New, InProgress, Resolved };
    }

    public static class Categories
    {
        public static readonly IReadOnlySet<string> All = new HashSet<string>
        {
            "blocks", "tests", "review", "chat", "stats", "account", "other"
        };
    }
}
