using Microsoft.AspNetCore.Http;

namespace Lexify.API.Requests.Feedback;

/// <summary>
/// Multipart form — attachments ride along with the fields, so this is bound with [FromForm]
/// rather than a JSON body.
/// </summary>
public sealed record SubmitFeedbackRequest(
    string Type,
    string? Category,
    string Subject,
    string Message,
    short? Rating,
    string? ContactEmail,
    bool Consent,
    List<IFormFile>? Attachments);
