using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Stats.Queries.GetConversationStats;

public sealed record GetConversationStatsQuery(Guid UserId) : IRequest<Result<ConversationStatsDto>>;

/// <summary>Aggregate "Talk to Lexi" practice stats for the Stats page.</summary>
/// <param name="AvgStars">Average recorded stars over ended sessions; null when none have a score yet.</param>
public sealed record ConversationStatsDto(
    int TotalSessions,
    int WordsPractised,
    double? AvgStars);
