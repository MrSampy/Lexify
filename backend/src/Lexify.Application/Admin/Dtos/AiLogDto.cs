namespace Lexify.Application.Admin.Dtos;

public sealed record AiLogDto(
    Guid Id,
    Guid? UserId,
    string CallType,
    string Provider,
    string Model,
    int? InputTokens,
    int? OutputTokens,
    int DurationMs,
    bool Success,
    string? ErrorMessage,
    DateTimeOffset CreatedAt);
