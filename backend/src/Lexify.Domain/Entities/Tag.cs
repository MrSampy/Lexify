namespace Lexify.Domain.Entities;

public sealed class Tag
{
    public int Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }

    private Tag() { }

    public Tag(Guid userId, string name)
    {
        UserId = userId;
        Name = name;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
