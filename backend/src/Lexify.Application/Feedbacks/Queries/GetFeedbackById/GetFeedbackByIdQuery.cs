using Lexify.Application.Common;
using Lexify.Application.Feedbacks.Dtos;
using MediatR;

namespace Lexify.Application.Feedbacks.Queries.GetFeedbackById;

/// <summary>Admin-only: the detail view includes the internal note.</summary>
public sealed record GetFeedbackByIdQuery(Guid FeedbackId) : IRequest<Result<FeedbackDetailDto>>;
