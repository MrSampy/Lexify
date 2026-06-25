namespace Lexify.Application.Blocks.Dtos;

public sealed record WordBlockDto(
    Guid Id,
    Guid UserId,
    short LanguageId,
    string Title,
    string? Description,
    int WordCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<string> Tags);
