using Lexify.Domain.Common;
using Lexify.Domain.Events;

namespace Lexify.Domain.Entities;

public sealed class Word
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; private set; }
    public Guid BlockId { get; private set; }
    public string Term { get; private set; } = default!;
    public string Translation { get; private set; } = default!;
    public string WordType { get; private set; } = default!;
    public string? Notes { get; private set; }
    public string? ExampleSentence { get; private set; }
    public bool ConfidenceFlag { get; private set; }
    public string? ConfidenceNote { get; private set; }
    public int SortOrder { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // SM-2 Spaced Repetition fields
    public double EaseFactor { get; private set; }
    public int IntervalDays { get; private set; }
    public int Repetitions { get; private set; }
    public DateTimeOffset NextReviewAt { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private Word() { }

    public Word(Guid blockId, string term, string translation,
        string wordType = WordTypes.Word, string? notes = null,
        string? exampleSentence = null, int sortOrder = 0)
    {
        Id = Guid.NewGuid();
        BlockId = blockId;
        Term = term;
        Translation = translation;
        WordType = wordType;
        Notes = notes;
        ExampleSentence = exampleSentence;
        SortOrder = sortOrder;
        CreatedAt = DateTimeOffset.UtcNow;
        EaseFactor = 2.5;
        IntervalDays = 1;
        Repetitions = 0;
        NextReviewAt = DateTimeOffset.UtcNow;
    }

    public static Word Create(
        Guid blockId,
        string term,
        string translation,
        string wordType = WordTypes.Word,
        string? notes = null,
        string? exampleSentence = null,
        int sortOrder = 0)
    {
        if (blockId == Guid.Empty) throw new DomainException("Block ID cannot be empty.");
        if (string.IsNullOrWhiteSpace(term)) throw new DomainException("Term cannot be empty.");
        if (string.IsNullOrWhiteSpace(translation)) throw new DomainException("Translation cannot be empty.");
        if (!WordTypes.All.Contains(wordType)) throw new DomainException($"Invalid word type: '{wordType}'.");
        return new Word(blockId, term, translation, wordType, notes, exampleSentence, sortOrder);
    }

    /// <summary>
    /// Applies SM-2 spaced repetition algorithm. Quality: 0-2 = fail, 3-5 = success.
    /// </summary>
    public void ApplyReviewResult(int quality)
    {
        if (quality < 0 || quality > 5) throw new DomainException("Quality must be between 0 and 5.");

        // EF always changes, clamped to minimum 1.3
        double newEaseFactor = EaseFactor + (0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02));
        EaseFactor = Math.Max(1.3, newEaseFactor);

        if (quality >= 3)
        {
            IntervalDays = Repetitions switch
            {
                0 => 1,
                1 => 6,
                _ => (int)Math.Round(IntervalDays * EaseFactor)
            };
            Repetitions++;
        }
        else
        {
            Repetitions = 0;
            IntervalDays = 1;
        }

        NextReviewAt = DateTimeOffset.UtcNow.AddDays(IntervalDays);
        _domainEvents.Add(new WordReviewedEvent(Id, quality, EaseFactor, IntervalDays));
    }

    public void UpdateTranslation(string translation, string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(translation)) throw new DomainException("Translation cannot be empty.");
        Translation = translation;
        if (notes is not null) Notes = notes;
    }

    public void SetConfidence(bool flag, string? note)
    {
        ConfidenceFlag = flag;
        ConfidenceNote = note;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    public static class WordTypes
    {
        public const string Word = "word";
        public const string Phrase = "phrase";
        public const string Idiom = "idiom";
        public const string Expression = "expression";

        public static readonly IReadOnlySet<string> All =
            new HashSet<string> { Word, Phrase, Idiom, Expression };
    }
}
