using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Review.Queries.GetWordHistory;

public sealed class GetWordHistoryQueryHandler(IReviewLogRepository reviewLogRepository)
    : IRequestHandler<GetWordHistoryQuery, Result<IReadOnlyList<WordReviewEntryDto>>>
{
    public async Task<Result<IReadOnlyList<WordReviewEntryDto>>> Handle(
        GetWordHistoryQuery request, CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(request.Limit, 1, 200);

        // The user filter doubles as the ownership check: someone else's word id yields [].
        var logs = await reviewLogRepository.GetByWordAsync(
            request.UserId, request.WordId, limit, cancellationToken);

        var dtos = logs
            .Select(l => new WordReviewEntryDto(
                l.Quality, l.Source, l.EaseFactorAfter, l.IntervalDaysAfter, l.ReviewedAt))
            .ToList();

        return Result.Ok<IReadOnlyList<WordReviewEntryDto>>(dtos);
    }
}
