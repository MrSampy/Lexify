using Lexify.Application.Common;
using Lexify.Application.Feedbacks.Common;
using Lexify.Application.Feedbacks.Dtos;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Feedbacks.Queries.GetFeedbackList;

public sealed class GetFeedbackListQueryHandler(IFeedbackRepository feedbackRepository)
    : IRequestHandler<GetFeedbackListQuery, Result<PagedResult<FeedbackListItemDto>>>
{
    public async Task<Result<PagedResult<FeedbackListItemDto>>> Handle(
        GetFeedbackListQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var (total, rows) = await feedbackRepository.GetPagedAsync(
            request.Type, request.Status, request.Category, request.Search,
            request.DateFrom, request.DateTo, page, pageSize, cancellationToken);

        var items = rows.Select(FeedbackMapping.ToListItem).ToList();

        return Result.Ok(new PagedResult<FeedbackListItemDto>(items, total, page, pageSize));
    }
}
