using Lexify.Domain.Common;
using Lexify.Domain.Events;
using Lexify.Domain.ValueObjects;

namespace Lexify.Domain.Entities;

public sealed class TestAttempt
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; private set; }
    public Guid TestId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }
    public double? Score { get; private set; }
    public int? TotalQuestions { get; private set; }
    public int? CorrectAnswers { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private TestAttempt() { }

    public TestAttempt(Guid testId, Guid userId)
    {
        Id = Guid.NewGuid();
        TestId = testId;
        UserId = userId;
        StartedAt = DateTimeOffset.UtcNow;
    }

    public static TestAttempt Start(Guid testId, Guid userId)
    {
        if (testId == Guid.Empty) throw new DomainException("Test ID cannot be empty.");
        if (userId == Guid.Empty) throw new DomainException("User ID cannot be empty.");
        return new TestAttempt(testId, userId);
    }

    public void Finish(TestScore score)
    {
        if (FinishedAt is not null) throw new DomainException("Attempt is already finished.");
        Score = score.Value;
        TotalQuestions = score.Total;
        CorrectAnswers = score.Correct;
        FinishedAt = DateTimeOffset.UtcNow;
        _domainEvents.Add(new TestCompletedEvent(Id, TestId, UserId, score));
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
