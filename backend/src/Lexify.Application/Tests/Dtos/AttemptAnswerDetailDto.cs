namespace Lexify.Application.Tests.Dtos;

public sealed record AttemptAnswerDetailDto(
    Guid QuestionId,
    string QuestionText,
    string QuestionType,
    string GivenAnswer,
    string CorrectAnswer,
    bool IsCorrect,
    int? TimeSpentMs);
