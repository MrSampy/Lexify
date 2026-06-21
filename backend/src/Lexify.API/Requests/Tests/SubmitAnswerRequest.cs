namespace Lexify.API.Requests.Tests;

public sealed record SubmitAnswerRequest(
    Guid QuestionId,
    string GivenAnswer,
    int? TimeSpentMs = null);
