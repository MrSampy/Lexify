using AutoMapper;
using Lexify.Application.Common;
using Lexify.Application.Words.Dtos;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Review.Queries.GetDueForReview;

public sealed class GetDueForReviewQueryHandler(
    IWordRepository wordRepository,
    IMapper mapper)
    : IRequestHandler<GetDueForReviewQuery, Result<IReadOnlyList<WordDto>>>
{
    public async Task<Result<IReadOnlyList<WordDto>>> Handle(
        GetDueForReviewQuery request, CancellationToken cancellationToken)
    {
        var words = await wordRepository.GetDueForReviewAsync(request.UserId, request.Limit, cancellationToken);
        return Result.Ok(mapper.Map<IReadOnlyList<WordDto>>(words));
    }
}
