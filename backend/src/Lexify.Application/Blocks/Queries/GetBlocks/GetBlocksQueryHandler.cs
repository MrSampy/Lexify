using AutoMapper;
using Lexify.Application.Blocks.Dtos;
using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Blocks.Queries.GetBlocks;

public sealed class GetBlocksQueryHandler(
    IWordBlockRepository blockRepository,
    ITagRepository tagRepository,
    IMapper mapper)
    : IRequestHandler<GetBlocksQuery, Result<PagedResult<WordBlockDto>>>
{
    public async Task<Result<PagedResult<WordBlockDto>>> Handle(
        GetBlocksQuery request, CancellationToken cancellationToken)
    {
        var skip = (request.Page - 1) * request.PageSize;

        var blocksTask = blockRepository.GetByUserIdAsync(
            request.UserId, request.LanguageId, request.Tag,
            skip, request.PageSize, cancellationToken);

        var totalTask = blockRepository.CountByUserIdAsync(
            request.UserId, request.LanguageId, request.Tag, cancellationToken);

        await Task.WhenAll(blocksTask, totalTask);

        var blocks = blocksTask.Result;
        var tagsMap = await tagRepository.GetTagNamesByBlockIdsAsync(
            blocks.Select(b => b.Id), cancellationToken);

        var dtos = blocks.Select(b =>
            mapper.Map<WordBlockDto>(b) with
            {
                Tags = tagsMap.TryGetValue(b.Id, out var tags) ? tags : []
            }).ToList();

        return Result.Ok(new PagedResult<WordBlockDto>(dtos, totalTask.Result, request.Page, request.PageSize));
    }
}
