namespace Lexify.Application.Tests.Dtos;

public sealed record TestDto(
    Guid Id,
    string Title,
    string Status,
    int? QuestionCount,
    DateTimeOffset CreatedAt,
    IReadOnlyList<QuestionDto> Questions);
