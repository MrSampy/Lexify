using Lexify.Domain.Common;
using Lexify.Domain.Events;
using Lexify.Domain.Services;

namespace Lexify.Domain.Entities;

public sealed class Word
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; private set; }
    public Guid BlockId { get; private set; }
    public string Term { get; private set; } = default!;
    /// <summary>The primary translation — used in search, tests, and review.</summary>
    public string Translation { get; private set; } = default!;
    /// <summary>Extra translation variants beyond the primary one (may be empty).</summary>
    public List<string> AlternativeTranslations { get; private set; } = [];
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
    /// Applies SM-2 spaced repetition result to this word and raises <see cref="WordReviewedEvent"/>.
    /// Delegates computation to <see cref="SpacedRepetitionService"/>.
    /// </summary>
    public void ApplyReviewResult(int quality)
    {
        var result = SpacedRepetitionService.Calculate(EaseFactor, IntervalDays, Repetitions, quality);

        EaseFactor = result.EaseFactor;
        IntervalDays = result.IntervalDays;
        Repetitions = result.Repetitions;
        NextReviewAt = result.NextReviewAt;

        _domainEvents.Add(new WordReviewedEvent(Id, quality, result.EaseFactor, result.IntervalDays));
    }

    public void UpdateTranslation(string translation, string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(translation)) throw new DomainException("Translation cannot be empty.");
        Translation = translation;
        if (notes is not null) Notes = notes;
    }

    public void UpdateDetails(string translation, string? notes, string? exampleSentence)
    {
        if (string.IsNullOrWhiteSpace(translation)) throw new DomainException("Translation cannot be empty.");
        Translation = translation;
        Notes = notes;
        ExampleSentence = exampleSentence;
    }

    public void SetConfidence(bool flag, string? note)
    {
        ConfidenceFlag = flag;
        ConfidenceNote = note;
    }

    /// <summary>Replaces alternative translations; blanks, duplicates, and copies of the primary are dropped.</summary>
    public void SetAlternativeTranslations(IEnumerable<string>? translations)
    {
        AlternativeTranslations = (translations ?? [])
            .Select(t => t.Trim())
            .Where(t => t.Length > 0 && !string.Equals(t, Translation, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
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
