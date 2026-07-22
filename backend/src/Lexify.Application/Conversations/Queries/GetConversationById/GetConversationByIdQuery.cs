using Lexify.Application.Common;
using Lexify.Application.Conversations.Dtos;
using MediatR;

namespace Lexify.Application.Conversations.Queries.GetConversationById;

public sealed record GetConversationByIdQuery(Guid ConversationId, Guid UserId)
    : IRequest<Result<ConversationDetailDto>>;
