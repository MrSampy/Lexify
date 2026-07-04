using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Words.Commands.UpdateWord;

public sealed record UpdateWordCommand(
    Guid WordId,
    string Translation,
    string? Notes,
    string? ExampleSentence,
    bool ConfidenceFlag,
    string? ConfidenceNote,
    IReadOnlyList<string>? AlternativeTranslations = null
) : IRequest<Result>;
