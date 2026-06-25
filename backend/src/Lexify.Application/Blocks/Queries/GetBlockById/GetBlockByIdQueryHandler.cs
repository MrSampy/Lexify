using AutoMapper;
using Lexify.Application.Blocks.Dtos;
using Lexify.Application.Common;
using Lexify.Application.Words.Dtos;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Blocks.Queries.GetBlockById;

public sealed class GetBlockByIdQueryHandler(
    IWordBlockRepository blockRepository,
    IWordRepository wordRepository,
    ITagRepository tagRepository,
    IMapper mapper)
    : IRequestHandler<GetBlockByIdQuery, Result<BlockDetailDto>>
{
    public async Task<Result<BlockDetailDto>> Handle(
        GetBlockByIdQuery request, CancellationToken cancellationToken)
    {
        var block = await blockRepository.GetByIdAsync(request.BlockId, cancellationToken);
        if (block is null)
            return Result.NotFound<BlockDetailDto>("Block not found.");

        if (block.UserId != request.UserId)
            return Result.Forbidden<BlockDetailDto>("You do not have access to this block.");

        var skip = (request.WordsPage - 1) * request.WordsPageSize;

        // Sequential awaits: all three repositories share the same scoped DbContext,
        // and concurrent EF Core operations on a single context cause a timeout.
        var words = await wordRepository.GetByBlockIdAsync(
            request.BlockId, null, skip, request.WordsPageSize, cancellationToken);
        var total = await wordRepository.CountByBlockIdAsync(
            request.BlockId, null, cancellationToken);
        var tags = await tagRepository.GetTagNamesByBlockIdAsync(request.BlockId, cancellationToken);

        var blockDto = mapper.Map<WordBlockDto>(block) with { Tags = tags };
        var wordDtos = mapper.Map<IReadOnlyList<WordDto>>(words);
        var pagedWords = new PagedResult<WordDto>(wordDtos, total, request.WordsPage, request.WordsPageSize);

        return Result.Ok(new BlockDetailDto(blockDto, pagedWords));
    }
}
