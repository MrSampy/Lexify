using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Stats.Queries.GetConversationStats;

public sealed class GetConversationStatsQueryHandler(
    IConversationRepository conversationRepository,
    IReviewLogRepository reviewLogRepository)
    : IRequestHandler<GetConversationStatsQuery, Result<ConversationStatsDto>>
{
    public async Task<Result<ConversationStatsDto>> Handle(
        GetConversationStatsQuery request, CancellationToken cancellationToken)
    {
        var totalSessions = await conversationRepository.CountEndedByUserIdAsync(
            request.UserId, cancellationToken);
        var wordsPractised = await reviewLogRepository.CountDistinctWordsBySourceAsync(
            request.UserId, WordReviewLog.Sources.Conversation, cancellationToken);
        var avgStars = await conversationRepository.GetAverageStarsAsync(
            request.UserId, cancellationToken);

        return Result.Ok(new ConversationStatsDto(
            totalSessions,
            wordsPractised,
            avgStars is null ? null : Math.Round(avgStars.Value, 1)));
    }
}
