namespace Lexify.Application.Admin.Dtos;

public sealed record SystemSettingDto(
    string Key,
    string Value,
    string ValueType,
    string? Description,
    DateTimeOffset UpdatedAt,
    Guid? UpdatedBy);
