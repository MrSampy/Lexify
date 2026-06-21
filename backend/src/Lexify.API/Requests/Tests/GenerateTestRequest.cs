namespace Lexify.API.Requests.Tests;

public sealed record GenerateTestRequest(
    IReadOnlyList<Guid> BlockIds,
    IReadOnlyList<string> QuestionTypes,
    int QuestionCount);
