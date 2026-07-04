using Lexify.Application.Behaviors;
using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Words.Commands.ImportWords;

public sealed record ImportWordsCommand(
    Guid BlockId,
    IReadOnlyList<ImportWordItem> Words
) : IRequest<Result<int>>, IInvalidatesBlocksCache;
