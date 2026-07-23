using Lexify.Application.Common;
using Lexify.Application.Feedbacks.Dtos;
using MediatR;

namespace Lexify.Application.Feedbacks.Queries.GetFeedbackAttachment;

/// <summary>Admin-only download of one attachment's bytes.</summary>
public sealed record GetFeedbackAttachmentQuery(Guid FeedbackId, Guid AttachmentId)
    : IRequest<Result<FeedbackAttachmentFileDto>>;
