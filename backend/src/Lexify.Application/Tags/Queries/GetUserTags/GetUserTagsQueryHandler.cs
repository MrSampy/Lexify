using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Tags.Queries.GetUserTags;

public sealed class GetUserTagsQueryHandler(ITagRepository tagRepository)
    : IRequestHandler<GetUserTagsQuery, Result<IReadOnlyList<string>>>
{
    public async Task<Result<IReadOnlyList<string>>> Handle(
        GetUserTagsQuery request, CancellationToken cancellationToken)
    {
        var tags = await tagRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        return Result.Ok<IReadOnlyList<string>>(tags.Select(t => t.Name).ToList());
    }
}
