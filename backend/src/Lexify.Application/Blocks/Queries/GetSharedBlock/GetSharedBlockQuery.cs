using Lexify.Application.Blocks.Common;
using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Blocks.Queries.GetSharedBlock;

/// <summary>Read-only preview of a shared block, addressed by its share token rather than its id.</summary>
public sealed record GetSharedBlockQuery(string Token) : IRequest<Result<SharedBlockDto>>;
