namespace Lexify.Application.Tests.Dtos;

public sealed record TestListItemDto(
    Guid Id,
    string Title,
    string Status,
    int? QuestionCount,
    DateTimeOffset CreatedAt);
