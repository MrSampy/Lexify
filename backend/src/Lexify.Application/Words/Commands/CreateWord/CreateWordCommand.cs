using Lexify.Application.Behaviors;
using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Words.Commands.CreateWord;

public sealed record CreateWordCommand(
    Guid BlockId,
    string Term,
    string Translation,
    string WordType,
    string? Notes,
    string? ExampleSentence,
    int SortOrder = 0,
    IReadOnlyList<string>? Synonyms = null
) : IRequest<Result<Guid>>, IInvalidatesBlocksCache;
