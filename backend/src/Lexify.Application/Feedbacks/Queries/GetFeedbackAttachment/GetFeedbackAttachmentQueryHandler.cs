using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Application.Feedbacks.Dtos;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Feedbacks.Queries.GetFeedbackAttachment;

public sealed class GetFeedbackAttachmentQueryHandler(
    IFeedbackRepository feedbackRepository,
    IFeedbackAttachmentStorage attachmentStorage)
    : IRequestHandler<GetFeedbackAttachmentQuery, Result<FeedbackAttachmentFileDto>>
{
    public async Task<Result<FeedbackAttachmentFileDto>> Handle(
        GetFeedbackAttachmentQuery request, CancellationToken cancellationToken)
    {
        var feedback = await feedbackRepository.GetByIdWithAttachmentsAsync(
            request.FeedbackId, cancellationToken);

        if (feedback is null)
            return Result.NotFound<FeedbackAttachmentFileDto>("Feedback not found.");

        // The attachment must belong to *this* submission — otherwise the feedback id in the route
        // would be decorative and any attachment id would be readable through any ticket.
        var attachment = feedback.Attachments.FirstOrDefault(a => a.Id == request.AttachmentId);
        if (attachment is null)
            return Result.NotFound<FeedbackAttachmentFileDto>("Attachment not found.");

        var content = await attachmentStorage.ReadAsync(attachment.StorageName, cancellationToken);
        if (content is null)
            return Result.NotFound<FeedbackAttachmentFileDto>("Attachment file is no longer available.");

        return Result.Ok(new FeedbackAttachmentFileDto(
            content, attachment.ContentType, attachment.FileName));
    }
}
