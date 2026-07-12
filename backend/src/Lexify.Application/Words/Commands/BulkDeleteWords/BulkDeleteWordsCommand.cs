using Lexify.Application.Behaviors;
using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Words.Commands.BulkDeleteWords;

public sealed record BulkDeleteWordsCommand(
    Guid BlockId,
    IReadOnlyList<Guid> WordIds
) : IRequest<Result<int>>, IInvalidatesBlocksCache;
