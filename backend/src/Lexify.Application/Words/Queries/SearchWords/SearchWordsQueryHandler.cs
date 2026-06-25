using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Words.Queries.SearchWords;

public sealed class SearchWordsQueryHandler(IWordRepository wordRepository)
    : IRequestHandler<SearchWordsQuery, Result<IReadOnlyList<SearchWordDto>>>
{
    public async Task<Result<IReadOnlyList<SearchWordDto>>> Handle(
        SearchWordsQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Q))
            return Result.Ok<IReadOnlyList<SearchWordDto>>([]);

        var results = await wordRepository.SearchAsync(
            request.UserId, request.Q, request.LanguageId,
            Math.Min(request.Limit, 50), cancellationToken);

        var dtos = results.Select(r => new SearchWordDto(
            r.WordId, r.BlockId, r.BlockTitle,
            r.Term, r.Translation, r.WordType, r.Rank
        )).ToList();

        return Result.Ok<IReadOnlyList<SearchWordDto>>(dtos);
    }
}
