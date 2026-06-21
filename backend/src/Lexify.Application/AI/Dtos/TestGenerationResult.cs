namespace Lexify.Application.AI.Dtos;

public sealed record TestGenerationResult(
    IReadOnlyList<GeneratedQuestion> Questions);

public sealed record GeneratedQuestion(
    string QuestionType,
    string Content,
    string? FillSentence,
    string TargetWordTerm,
    IReadOnlyList<GeneratedOption> Options);

public sealed record GeneratedOption(
    string Text,
    bool IsCorrect);
