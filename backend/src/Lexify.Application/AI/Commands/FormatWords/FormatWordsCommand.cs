using MediatR;

namespace Lexify.Application.AI.Commands.FormatWords;

public sealed record FormatWordsCommand(
    string RawText,
    string TargetLanguage,
    string NativeLanguage)
    : IStreamRequest<FormatWordsSseEvent>;
