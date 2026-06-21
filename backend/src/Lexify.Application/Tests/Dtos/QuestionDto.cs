namespace Lexify.Application.Tests.Dtos;

public sealed record QuestionDto(
    Guid Id,
    string QuestionType,
    string QuestionText,
    int SortOrder,
    IReadOnlyList<QuestionOptionDto> Options);
