namespace Lexify.Application.Admin.Dtos;

public sealed record AiStatsDto(
    int TotalCalls,
    int SuccessfulCalls,
    int FailedCalls,
    double ErrorRatePercent,
    int FallbackCount,
    double AverageResponseTimeMs,
    IReadOnlyList<AiCallTypeStatDto> ByCallType,
    IReadOnlyList<AiProviderStatDto> ByProvider);

public sealed record AiCallTypeStatDto(
    string CallType,
    int Count,
    double AvgDurationMs,
    int ErrorCount);

public sealed record AiProviderStatDto(
    string Provider,
    int Count,
    double AvgDurationMs,
    int ErrorCount);
