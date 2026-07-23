using Lexify.Application.Common;
using Lexify.Application.Feedbacks.Dtos;
using MediatR;

namespace Lexify.Application.Feedbacks.Queries.GetFeedbackList;

/// <summary>Admin triage list. <paramref name="Search"/> matches a ticket number or subject/message text.</summary>
public sealed record GetFeedbackListQuery(
    string? Type,
    string? Status,
    string? Category,
    string? Search,
    DateTimeOffset? DateFrom,
    DateTimeOffset? DateTo,
    int Page = 1,
    int PageSize = 50)
    : IRequest<Result<PagedResult<FeedbackListItemDto>>>;
