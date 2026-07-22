using Lexify.Application.Common;
using Lexify.Application.Conversations.Dtos;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Conversations.Queries.GetConversationById;

public sealed class GetConversationByIdQueryHandler(
    IConversationRepository conversationRepository,
    IWordRepository wordRepository)
    : IRequestHandler<GetConversationByIdQuery, Result<ConversationDetailDto>>
{
    public async Task<Result<ConversationDetailDto>> Handle(
        GetConversationByIdQuery request, CancellationToken cancellationToken)
    {
        var conversation = await conversationRepository.GetByIdWithMessagesAsync(
            request.ConversationId, cancellationToken);

        if (conversation is null || conversation.UserId != request.UserId)
            return Result.NotFound<ConversationDetailDto>("Conversation not found.");

        var targetWords = new List<TargetWordDto>();
        foreach (var wordId in conversation.TargetWordIds)
        {
            var word = await wordRepository.GetByIdAsync(wordId, cancellationToken);
            if (word is not null)
                targetWords.Add(new TargetWordDto(word.Id, word.Term, word.Translation));
        }

        var messages = conversation.Messages
            .OrderBy(m => m.SortOrder)
            .Select(m => new ConversationMessageDto(m.Id, m.Role, m.Content, m.CreatedAt))
            .ToList();

        return Result.Ok(new ConversationDetailDto(
            conversation.Id,
            conversation.LanguageId,
            conversation.Title,
            conversation.Scenario,
            conversation.Status,
            targetWords,
            messages));
    }
}
