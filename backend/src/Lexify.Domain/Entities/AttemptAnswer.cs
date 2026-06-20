namespace Lexify.Domain.Entities;

public sealed class AttemptAnswer
{
    public Guid Id { get; private set; }
    public Guid AttemptId { get; private set; }
    public Guid QuestionId { get; private set; }
    public string GivenAnswer { get; private set; } = default!;
    public bool IsCorrect { get; private set; }
    public int? TimeSpentMs { get; private set; }
    public DateTimeOffset AnsweredAt { get; private set; }

    private AttemptAnswer() { }

    public AttemptAnswer(Guid attemptId, Guid questionId, string givenAnswer,
        bool isCorrect, int? timeSpentMs = null)
    {
        Id = Guid.NewGuid();
        AttemptId = attemptId;
        QuestionId = questionId;
        GivenAnswer = givenAnswer;
        IsCorrect = isCorrect;
        TimeSpentMs = timeSpentMs;
        AnsweredAt = DateTimeOffset.UtcNow;
    }
}
