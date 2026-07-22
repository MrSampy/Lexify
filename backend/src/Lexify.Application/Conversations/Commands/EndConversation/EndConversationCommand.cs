using Lexify.Application.Common;
using Lexify.Application.Conversations.Dtos;
using MediatR;

namespace Lexify.Application.Conversations.Commands.EndConversation;

public sealed record EndConversationCommand(Guid ConversationId, Guid UserId)
    : IRequest<Result<ConversationSummaryDto>>;
