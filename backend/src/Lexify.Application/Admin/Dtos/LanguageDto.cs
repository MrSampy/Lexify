namespace Lexify.Application.Admin.Dtos;

public sealed record LanguageDto(
    short Id,
    string Code,
    string Name,
    string NativeName,
    bool IsActive,
    short SortOrder);
