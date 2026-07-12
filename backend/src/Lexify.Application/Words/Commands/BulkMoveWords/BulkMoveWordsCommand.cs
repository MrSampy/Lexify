using Lexify.Application.Behaviors;
using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Words.Commands.BulkMoveWords;

public sealed record BulkMoveWordsCommand(
    Guid BlockId,
    Guid TargetBlockId,
    IReadOnlyList<Guid> WordIds
) : IRequest<Result<int>>, IInvalidatesBlocksCache;
