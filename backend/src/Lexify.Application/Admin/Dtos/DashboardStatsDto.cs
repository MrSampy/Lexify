namespace Lexify.Application.Admin.Dtos;

public sealed record DashboardStatsDto(
    int TotalUsers,
    int ActiveUsersLast7Days,
    int ActiveUsersLast30Days,
    int TotalWords,
    int TotalWordBlocks,
    int TotalTests,
    int AiCallsLast24Hours,
    int AiCallsLast7Days,
    IReadOnlyList<LanguageStatDto> TopLanguages);
