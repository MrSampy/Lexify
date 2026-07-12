using AutoMapper;
using Lexify.Application.Common;
using Lexify.Application.Words.Dtos;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Review.Queries.GetDueForReview;

public sealed class GetDueForReviewQueryHandler(
    IWordRepository wordRepository,
    IWordBlockRepository blockRepository,
    IMapper mapper)
    : IRequestHandler<GetDueForReviewQuery, Result<IReadOnlyList<WordDto>>>
{
    public async Task<Result<IReadOnlyList<WordDto>>> Handle(
        GetDueForReviewQuery request, CancellationToken cancellationToken)
    {
        var words = await wordRepository.GetDueForReviewAsync(request.UserId, request.Limit, cancellationToken);

        // Review cards need the term's language (for TTS voice selection), which lives on the block.
        var languageIds = await blockRepository.GetLanguageIdsAsync(
            words.Select(w => w.BlockId).Distinct().ToArray(), cancellationToken);

        var dtos = mapper.Map<IReadOnlyList<WordDto>>(words)
            .Select(dto => languageIds.TryGetValue(dto.BlockId, out var languageId)
                ? dto with { LanguageId = languageId }
                : dto)
            .ToList();

        return Result.Ok<IReadOnlyList<WordDto>>(dtos);
    }
}
