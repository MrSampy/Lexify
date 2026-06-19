using Lexify.Domain.Common;

namespace Lexify.Domain.Entities;

public sealed class Test : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = default!;
    public string Status { get; private set; } = default!;
    public int? QuestionCount { get; private set; }

    private Test() { }

    public Test(Guid userId, string title)
    {
        UserId = userId;
        Title = title;
        Status = Statuses.Generating;
    }

    public void MarkReady(int questionCount)
    {
        Status = Statuses.Ready;
        QuestionCount = questionCount;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Archive()
    {
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
