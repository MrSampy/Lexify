using Lexify.Domain.Common;

namespace Lexify.Domain.Entities;

public sealed class Test : BaseEntity
{
    private readonly List<Question> _questions = [];

    public Guid UserId { get; private set; }
    public string Title { get; private set; } = default!;
    public string Status { get; private set; } = default!;
    public int? QuestionCount { get; private set; }

    public bool IsArchived => Status == Statuses.Archived;

    public IReadOnlyCollection<Question> Questions => _questions.AsReadOnly();

    private Test() { }

    public Test(Guid userId, string title)
    {
        UserId = userId;
        Title = title;
        Status = Statuses.Generating;
    }

    public static Test Create(Guid userId, string title)
    {
        if (userId == Guid.Empty) throw new DomainException("User ID cannot be empty.");
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Test title cannot be empty.");
        return new Test(userId, title);
    }

    public void MarkReady(int questionCount)
    {
        if (Status != Statuses.Generating)
            throw new DomainException("Only a generating test can be marked as ready.");
        Status = Statuses.Ready;
        QuestionCount = questionCount;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Archive()
    {
        if (Status == Statuses.Archived)
            throw new DomainException("Test is already archived.");
        Status = Statuses.Archived;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static class Statuses
    {
        public const string Generating = "generating";
        public const string Ready = "ready";
        public const string Archived = "archived";
    }
}
