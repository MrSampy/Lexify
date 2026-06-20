namespace Lexify.Domain.Entities;

public sealed class TestAttempt
{
    public Guid Id { get; private set; }
    public Guid TestId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }
    public double? Score { get; private set; }
    public int? TotalQuestions { get; private set; }
    public int? CorrectAnswers { get; private set; }

    private TestAttempt() { }

    public TestAttempt(Guid testId, Guid userId)
    {
        Id = Guid.NewGuid();
        TestId = testId;
        UserId = userId;
        StartedAt = DateTimeOffset.UtcNow;
    }

    public void Complete(double score, int totalQuestions, int correctAnswers)
    {
        Score = score;
        TotalQuestions = totalQuestions;
        CorrectAnswers = correctAnswers;
        FinishedAt = DateTimeOffset.UtcNow;
    }
}
