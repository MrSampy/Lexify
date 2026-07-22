using Lexify.Application.Common;
using Lexify.Application.Conversations.Dtos;
using MediatR;

namespace Lexify.Application.Conversations.Queries.GetConversations;

public sealed record GetConversationsQuery(Guid UserId, int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<ConversationListItemDto>>>;
