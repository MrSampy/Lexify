using Lexify.Application.Common;
using Lexify.Application.Feedbacks.Dtos;
using MediatR;

namespace Lexify.Application.Feedbacks.Queries.GetMyFeedback;

public sealed record GetMyFeedbackQuery(Guid UserId, int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<FeedbackListItemDto>>>;
