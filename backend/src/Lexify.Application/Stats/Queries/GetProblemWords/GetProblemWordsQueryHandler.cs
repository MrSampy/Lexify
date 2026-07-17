using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Stats.Queries.GetProblemWords;

public sealed class GetProblemWordsQueryHandler(IWordRepository wordRepository)
    : IRequestHandler<GetProblemWordsQuery, Result<IReadOnlyList<ProblemWordDto>>>
{
    public async Task<Result<IReadOnlyList<ProblemWordDto>>> Handle(
        GetProblemWordsQuery request, CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(request.Limit, 1, 100);
        var words = await wordRepository.GetProblemWordsAsync(request.UserId, limit, cancellationToken);

        var dtos = words
            .Select(w => new ProblemWordDto(
                w.WordId, w.BlockId, w.BlockTitle, w.Term, w.Translation,
                w.LapseCount, w.EaseFactor, w.IntervalDays, w.NextReviewAt, w.ConfidenceFlag))
            .ToList();

        return Result.Ok<IReadOnlyList<ProblemWordDto>>(dtos);
    }
}
