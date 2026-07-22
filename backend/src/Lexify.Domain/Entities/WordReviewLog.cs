namespace Lexify.Domain.Entities;

/// <summary>
/// An immutable record of a single spaced-repetition review of a word — one row per rating, whether it
/// came from a review session or from finishing a test. Unlike the in-place SM-2 fields on <see cref="Word"/>,
/// this preserves history, which is what powers streaks, activity heatmaps, and accuracy trends. Rows are
/// never updated or deleted in normal operation.
/// </summary>
public sealed class WordReviewLog
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid WordId { get; private set; }
    public Guid BlockId { get; private set; }
    public short LanguageId { get; private set; }

    /// <summary>SM-2 quality 0–5 the user (or the test grader) gave for this review.</summary>
    public int Quality { get; private set; }

    /// <summary>Where the review came from — see <see cref="Sources"/>.</summary>
    public string Source { get; private set; } = default!;

    /// <summary>The word's SM-2 state immediately after this review, snapshotted for retention analytics.</summary>
    public double EaseFactorAfter { get; private set; }
    public int IntervalDaysAfter { get; private set; }

    public DateTimeOffset ReviewedAt { get; private set; }

    private WordReviewLog() { }

    public WordReviewLog(Guid userId, Guid wordId, Guid blockId, short languageId,
        int quality, string source, double easeFactorAfter, int intervalDaysAfter)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        WordId = wordId;
        BlockId = blockId;
        LanguageId = languageId;
        Quality = quality;
        Source = source;
        EaseFactorAfter = easeFactorAfter;
        IntervalDaysAfter = intervalDaysAfter;
        ReviewedAt = DateTimeOffset.UtcNow;
    }

    public static class Sources
    {
        public const string Review = "review";
        public const string Test = "test";

        /// <summary>The word was produced (or attempted) in a "Talk to Lexi" conversation.</summary>
        public const string Conversation = "conversation";

        public static readonly IReadOnlySet<string> All = new HashSet<string> { Review, Test, Conversation };
    }
}
