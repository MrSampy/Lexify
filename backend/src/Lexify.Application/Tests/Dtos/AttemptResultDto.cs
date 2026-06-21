namespace Lexify.Application.Tests.Dtos;

public sealed record AttemptResultDto(
    Guid AttemptId,
    Guid TestId,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    double Score,
    int TotalQuestions,
    int CorrectAnswers,
    IReadOnlyList<AttemptAnswerDetailDto> Answers);
