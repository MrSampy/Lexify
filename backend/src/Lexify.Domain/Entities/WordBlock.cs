using Lexify.Domain.Common;

namespace Lexify.Domain.Entities;

public sealed class WordBlock : BaseEntity
{
    public Guid UserId { get; private set; }
    public short LanguageId { get; private set; }
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public int WordCount { get; private set; }

    private WordBlock() { }

    public WordBlock(Guid userId, short languageId, string title, string? description = null)
    {
        UserId = userId;
        LanguageId = languageId;
        Title = title;
        Description = description;
        WordCount = 0;
    }

    public void UpdateDetails(string title, string? description)
    {
        Title = title;
        Description = description;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
