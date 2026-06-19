namespace Lexify.Domain.Entities;

public sealed class Word
{
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

    public void SetConfidence(bool flag, string? note)
    {
        ConfidenceFlag = flag;
        ConfidenceNote = note;
    }

    public void ApplySM2(double easeFactor, int intervalDays, int repetitions, DateTimeOffset nextReviewAt)
    {
        EaseFactor = easeFactor;
        IntervalDays = intervalDays;
        Repetitions = repetitions;
        NextReviewAt = nextReviewAt;
    }

    public static class WordTypes
    {
        public const string Word = "word";
        public const string Phrase = "phrase";
        public const string Idiom = "idiom";
        public const string Expression = "expression";
    }
}
