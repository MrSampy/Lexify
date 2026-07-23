namespace Lexify.Application.Admin.Dtos;

public sealed record AdminUserDto(
    Guid Id,
    string Email,
    string? DisplayName,
    string Role,
    string Status,
    DateTimeOffset? LastActiveAt,
    DateTimeOffset? EmailVerifiedAt,
    DateTimeOffset CreatedAt,
    int BlockCount,
    int WordCount,
    int TestCount);
