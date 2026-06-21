namespace Lexify.Domain.Repositories;

public sealed record AdminUserEntry(
    Guid Id,
    string Email,
    string? DisplayName,
    string Role,
    string Status,
    DateTimeOffset? LastActiveAt,
    DateTimeOffset CreatedAt,
    int BlockCount,
    int WordCount,
    int TestCount);
