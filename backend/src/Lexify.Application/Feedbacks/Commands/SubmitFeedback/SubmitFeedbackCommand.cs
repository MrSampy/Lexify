using Lexify.Application.Common;
using Lexify.Application.Feedbacks.Common;
using Lexify.Application.Feedbacks.Dtos;
using MediatR;

namespace Lexify.Application.Feedbacks.Commands.SubmitFeedback;

/// <param name="Rating">1–5, required for type <c>review</c> and rejected for every other type.</param>
/// <param name="ContactEmail">Reply address; falls back to the account email when blank.</param>
/// <param name="Consent">GDPR consent checkbox. Re-checked server-side — the client can lie.</param>
public sealed record SubmitFeedbackCommand(
    Guid UserId,
    string Type,
    string? Category,
    string Subject,
    string Message,
    short? Rating,
    string? ContactEmail,
    bool Consent,
    IReadOnlyList<FeedbackAttachmentUpload> Attachments)
    : IRequest<Result<SubmitFeedbackResultDto>>;
