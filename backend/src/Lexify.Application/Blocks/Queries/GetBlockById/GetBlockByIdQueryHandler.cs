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

        var wordsTask = wordRepository.GetByBlockIdAsync(
            request.BlockId, null, skip, request.WordsPageSize, cancellationToken);

        var totalTask = wordRepository.CountByBlockIdAsync(
            request.BlockId, null, cancellationToken);

        var tagsTask = tagRepository.GetTagNamesByBlockIdAsync(request.BlockId, cancellationToken);

        await Task.WhenAll(wordsTask, totalTask, tagsTask);

        var blockDto = mapper.Map<WordBlockDto>(block) with { Tags = tagsTask.Result };
        var wordDtos = mapper.Map<IReadOnlyList<WordDto>>(wordsTask.Result);
        var pagedWords = new PagedResult<WordDto>(wordDtos, totalTask.Result, request.WordsPage, request.WordsPageSize);

        return Result.Ok(new BlockDetailDto(blockDto, pagedWords));
    }
}
