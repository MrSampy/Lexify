using AutoMapper;
using Lexify.Application.Common;
using Lexify.Application.Words.Dtos;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Words.Queries.GetWordsByBlock;

public sealed class GetWordsByBlockQueryHandler(
    IWordBlockRepository blockRepository,
    IWordRepository wordRepository,
    IMapper mapper)
    : IRequestHandler<GetWordsByBlockQuery, Result<PagedResult<WordDto>>>
{
    public async Task<Result<PagedResult<WordDto>>> Handle(
        GetWordsByBlockQuery request, CancellationToken cancellationToken)
    {
        var block = await blockRepository.GetByIdAsync(request.BlockId, cancellationToken);
        if (block is null)
            return Result.NotFound<PagedResult<WordDto>>("Block not found.");

        if (block.UserId != request.UserId)
            return Result.Forbidden<PagedResult<WordDto>>("You do not have access to this block.");

        var skip = (request.Page - 1) * request.PageSize;

        var words = await wordRepository.GetByBlockIdAsync(
            request.BlockId, request.Search, skip, request.PageSize, cancellationToken);

        var total = await wordRepository.CountByBlockIdAsync(
            request.BlockId, request.Search, cancellationToken);

        var dtos = mapper.Map<IReadOnlyList<WordDto>>(words);
        return Result.Ok(new PagedResult<WordDto>(dtos, total, request.Page, request.PageSize));
    }
}
