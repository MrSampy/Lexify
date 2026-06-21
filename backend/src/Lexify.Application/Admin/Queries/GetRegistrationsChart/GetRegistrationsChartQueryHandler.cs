using Lexify.Application.Admin.Dtos;
using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Admin.Queries.GetRegistrationsChart;

public sealed class GetRegistrationsChartQueryHandler(IAdminStatsRepository adminStats)
    : IRequestHandler<GetRegistrationsChartQuery, Result<IReadOnlyList<RegistrationDataPointDto>>>
{
    public async Task<Result<IReadOnlyList<RegistrationDataPointDto>>> Handle(
        GetRegistrationsChartQuery request, CancellationToken cancellationToken)
    {
        var points = await adminStats.GetRegistrationsByDayAsync(request.Days, cancellationToken);

        IReadOnlyList<RegistrationDataPointDto> result = points
            .Select(p => new RegistrationDataPointDto(p.Date, p.Count))
            .ToList();

        return Result.Ok(result);
    }
}
