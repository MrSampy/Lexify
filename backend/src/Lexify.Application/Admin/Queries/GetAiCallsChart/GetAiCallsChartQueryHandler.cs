using Lexify.Application.Admin.Dtos;
using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Admin.Queries.GetAiCallsChart;

public sealed class GetAiCallsChartQueryHandler(IAdminStatsRepository adminStats)
    : IRequestHandler<GetAiCallsChartQuery, Result<IReadOnlyList<AiCallDataPointDto>>>
{
    public async Task<Result<IReadOnlyList<AiCallDataPointDto>>> Handle(
        GetAiCallsChartQuery request, CancellationToken cancellationToken)
    {
        var points = await adminStats.GetAiCallsByHourAsync(request.Hours, cancellationToken);

        IReadOnlyList<AiCallDataPointDto> result = points
            .Select(p => new AiCallDataPointDto(p.HourStart, p.Count))
            .ToList();

        return Result.Ok(result);
    }
}
