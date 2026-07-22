using Lexify.Application.Common;
using Lexify.Application.Conversations.Dtos;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Conversations.Queries.GetConversations;

public sealed class GetConversationsQueryHandler(IConversationRepository conversationRepository)
    : IRequestHandler<GetConversationsQuery, Result<PagedResult<ConversationListItemDto>>>
{
    public async Task<Result<PagedResult<ConversationListItemDto>>> Handle(
        GetConversationsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var total = await conversationRepository.CountByUserIdAsync(request.UserId, cancellationToken);
        var rows = await conversationRepository.GetListByUserIdAsync(
            request.UserId, (page - 1) * pageSize, pageSize, cancellationToken);

        var items = rows
            .Select(r => new ConversationListItemDto(
                r.Id, r.LanguageId, r.Title, r.Scenario, r.Status,
                r.CreatedAt, r.EndedAt, r.MessageCount, r.Points, r.Stars))
            .ToList();

        return Result.Ok(new PagedResult<ConversationListItemDto>(items, total, page, pageSize));
    }
}
