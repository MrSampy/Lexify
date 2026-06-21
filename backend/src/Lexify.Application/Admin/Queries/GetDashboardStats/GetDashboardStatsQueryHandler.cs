using Lexify.Application.Admin.Dtos;
using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Admin.Queries.GetDashboardStats;

public sealed class GetDashboardStatsQueryHandler(IAdminStatsRepository adminStats)
    : IRequestHandler<GetDashboardStatsQuery, Result<DashboardStatsDto>>
{
    public async Task<Result<DashboardStatsDto>> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var since30Days = DateTimeOffset.UtcNow.AddDays(-30);
        var since24Hours = DateTimeOffset.UtcNow.AddHours(-24);

        // Sequential queries — EF Core DbContext is not thread-safe
        var totalUsers = await adminStats.CountUsersAsync(cancellationToken);
        var activeUsers = await adminStats.CountActiveUsersAsync(since30Days, cancellationToken);
        var totalWords = await adminStats.CountWordsAsync(cancellationToken);
        var totalBlocks = await adminStats.CountWordBlocksAsync(cancellationToken);
        var totalTests = await adminStats.CountTestsAsync(cancellationToken);
        var aiCalls = await adminStats.CountAiCallsAsync(since24Hours, cancellationToken);
        var topLanguages = await adminStats.GetTopLanguagesByBlockCountAsync(5, cancellationToken);

        var dto = new DashboardStatsDto(
            TotalUsers: totalUsers,
            ActiveUsersLast30Days: activeUsers,
            TotalWords: totalWords,
            TotalWordBlocks: totalBlocks,
            TotalTests: totalTests,
            AiCallsLast24Hours: aiCalls,
            TopLanguages: topLanguages
                .Select(l => new LanguageStatDto(l.Code, l.Name, l.BlockCount))
                .ToList());

        return Result.Ok(dto);
    }
}
