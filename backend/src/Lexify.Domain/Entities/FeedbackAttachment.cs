using Lexify.Domain.Common;

namespace Lexify.Domain.Entities;

/// <summary>
/// A file attached to a <see cref="Feedback"/> submission — typically a screenshot of the bug.
/// The bytes live on disk, not in the database: <see cref="StorageName"/> is a generated
/// <c>{guid}{ext}</c> and is the only thing ever joined onto a filesystem path. The user's original
/// <see cref="FileName"/> is kept for display only, so a crafted name can never escape the directory.
/// </summary>
public sealed class FeedbackAttachment
{
    public Guid Id { get; private set; }
    public Guid FeedbackId { get; private set; }
    public string FileName { get; private set; } = default!;
    public string ContentType { get; private set; } = default!;
    public long SizeBytes { get; private set; }
    public string StorageName { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }

    private FeedbackAttachment() { }

    internal FeedbackAttachment(
        Guid feedbackId, string fileName, string contentType, long sizeBytes, string storageName)
    {
        if (string.IsNullOrWhiteSpace(storageName))
            throw new DomainException("Attachment storage name cannot be empty.");

        Id = Guid.NewGuid();
        FeedbackId = feedbackId;
        FileName = fileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        StorageName = storageName;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
