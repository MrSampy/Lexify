using Lexify.Application.Blocks.Common;
using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Blocks.Queries.GetBlockShare;

/// <summary>The block's current share link, or a null value when sharing is off.</summary>
public sealed record GetBlockShareQuery(Guid BlockId) : IRequest<Result<BlockShareDto?>>;
