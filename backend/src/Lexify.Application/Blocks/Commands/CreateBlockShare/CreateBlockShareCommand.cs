using Lexify.Application.Blocks.Common;
using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Blocks.Commands.CreateBlockShare;

/// <summary>Turns sharing on for a block and returns its link. Idempotent — an existing live link is reused.</summary>
public sealed record CreateBlockShareCommand(Guid BlockId) : IRequest<Result<BlockShareDto>>;
