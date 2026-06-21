using Lexify.Application.AI.Dtos;

namespace Lexify.Application.AI.Commands.FormatWords;

public sealed record FormatWordsSseEvent(
    string EventType,
    string? Chunk = null,
    FormatWordsResult? Result = null,
    string? ErrorMessage = null);
