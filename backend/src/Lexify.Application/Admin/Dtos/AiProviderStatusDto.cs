namespace Lexify.Application.Admin.Dtos;

public sealed record AiProviderStatusDto(
    string Provider,
    string Status,
    int RecentCallCount,
    double RecentSuccessRatePercent,
    DateTimeOffset? LastCallAt);
