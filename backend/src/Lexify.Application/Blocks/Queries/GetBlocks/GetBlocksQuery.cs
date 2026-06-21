using Lexify.Application.Behaviors;
using Lexify.Application.Blocks.Dtos;
using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Blocks.Queries.GetBlocks;

public sealed record GetBlocksQuery(
    Guid UserId,
    short? LanguageId,
    string? Tag,
    int Page,
    int PageSize
) : IRequest<Result<PagedResult<WordBlockDto>>>, ICacheable
{
    public string CacheKey =>
        $"blocks:{UserId}:{LanguageId}:{Tag}:{Page}:{PageSize}";

    public TimeSpan CacheDuration => TimeSpan.FromMinutes(5);
}
