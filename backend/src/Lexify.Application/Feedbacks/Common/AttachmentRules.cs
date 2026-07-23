namespace Lexify.Application.Feedbacks.Common;

/// <summary>A file the user attached to a submission, already buffered by the controller.</summary>
public sealed record FeedbackAttachmentUpload(string FileName, byte[] Content);

/// <summary>The type an attachment's own bytes claim to be.</summary>
public sealed record SniffedType(string ContentType, string Extension);

/// <summary>
/// Limits and content sniffing for feedback attachments. The declared <c>Content-Type</c> and the
/// filename extension are both attacker-controlled, so the accepted type is decided from the leading
/// bytes of the file itself and the on-disk extension is derived from that decision.
/// </summary>
public static class AttachmentRules
{
    public const int MaxCount = 3;
    public const long MaxSizeBytes = 5 * 1024 * 1024;

    /// <summary>What the file picker advertises; the server still re-decides from the bytes.</summary>
    public const string AcceptedContentTypes = "image/png, image/jpeg, image/webp, application/pdf";

    private static ReadOnlySpan<byte> PngSignature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>
    /// Identifies the content by magic bytes, or returns null when it is not an accepted type.
    /// </summary>
    public static SniffedType? Sniff(ReadOnlySpan<byte> content)
    {
        if (content.Length < 12)
            return null;

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (content[..8].SequenceEqual(PngSignature))
            return new SniffedType("image/png", ".png");

        // JPEG: FF D8 FF
        if (content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF)
            return new SniffedType("image/jpeg", ".jpg");

        // WebP: "RIFF" ???? "WEBP"
        if (content[..4].SequenceEqual("RIFF"u8) && content[8..12].SequenceEqual("WEBP"u8))
            return new SniffedType("image/webp", ".webp");

        // PDF: "%PDF-"
        if (content[..5].SequenceEqual("%PDF-"u8))
            return new SniffedType("application/pdf", ".pdf");

        return null;
    }
}
