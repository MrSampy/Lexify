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
        var conversations = await conversationRepository.GetByUserIdAsync(
            request.UserId, (page - 1) * pageSize, pageSize, cancellationToken);

        var items = conversations
            .Select(c => new ConversationListItemDto(
                c.Id, c.LanguageId, c.Title, c.Scenario, c.Status,
                c.CreatedAt, c.EndedAt, c.Messages.Count))
            .ToList();

        return Result.Ok(new PagedResult<ConversationListItemDto>(items, total, page, pageSize));
    }
}
