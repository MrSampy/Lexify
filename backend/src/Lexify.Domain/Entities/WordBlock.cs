using Lexify.Domain.Common;
using Lexify.Domain.Events;

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

    public static WordBlock Create(Guid userId, short languageId, string title, string? description = null)
    {
        if (userId == Guid.Empty) throw new DomainException("User ID cannot be empty.");
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Block title cannot be empty.");
        return new WordBlock(userId, languageId, title, description);
    }

    public void Rename(string newTitle, string? newDescription = null)
    {
        if (string.IsNullOrWhiteSpace(newTitle)) throw new DomainException("Block title cannot be empty.");
        Title = newTitle;
        Description = newDescription ?? Description;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public Word AddWord(
        string term,
        string translation,
        string wordType = Word.WordTypes.Word,
        string? notes = null,
        string? exampleSentence = null,
        int sortOrder = 0)
    {
        var word = Word.Create(Id, term, translation, wordType, notes, exampleSentence, sortOrder);
        AddDomainEvent(new WordCreatedEvent(word.Id, Id));
        return word;
    }

    public void UpdateDetails(string title, string? description)
    {
        Rename(title, description);
    }
}
