using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Stats.Queries.GetMastery;

public sealed class GetMasteryQueryHandler(IWordRepository wordRepository)
    : IRequestHandler<GetMasteryQuery, Result<MasteryDto>>
{
    public async Task<Result<MasteryDto>> Handle(GetMasteryQuery request, CancellationToken cancellationToken)
    {
        var counts = await wordRepository.GetMasteryCountsAsync(request.UserId, cancellationToken);
        return Result.Ok(new MasteryDto(counts.New, counts.Learning, counts.Young, counts.Mature));
    }
}
