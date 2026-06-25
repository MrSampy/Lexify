using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Tags.Queries.GetUserTags;

public sealed record GetUserTagsQuery(Guid UserId) : IRequest<Result<IReadOnlyList<string>>>;
