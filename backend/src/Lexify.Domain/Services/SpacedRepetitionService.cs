using Lexify.Domain.Common;

namespace Lexify.Domain.Services;

/// <summary>
/// Pure SM-2 algorithm implementation.
/// See: https://www.supermemo.com/en/blog/application-of-a-computer-to-improve-the-results-obtained-in-working-with-the-supermemo-method
/// </summary>
public sealed class SpacedRepetitionService
{
    /// <summary>Minimum quality that counts as a successful recall (0–2 = lapse, 3–5 = success).</summary>
    public const int RecallThreshold = 3;

    /// <summary>
    /// Computes new SM-2 state given the current state and review quality (0–5).
    /// Quality: 0–2 = failed recall, 3–5 = successful recall.
    /// </summary>
    public static Sm2Result Calculate(double easeFactor, int intervalDays, int repetitions, int quality)
    {
        if (quality < 0 || quality > 5)
            throw new DomainException("Quality must be between 0 and 5.");

        // Ease factor always changes, minimum 1.3
        double newEF = easeFactor + (0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02));
        newEF = Math.Max(1.3, newEF);

        int newInterval;
        int newRepetitions;

        if (quality >= RecallThreshold)
        {
            newInterval = repetitions switch
            {
                0 => 1,
                1 => 6,
                _ => (int)Math.Round(intervalDays * newEF)
            };
            newRepetitions = repetitions + 1;
        }
        else
        {
            // Failed recall: reset streak, retry tomorrow
            newInterval = 1;
            newRepetitions = 0;
        }

        return new Sm2Result(newEF, newInterval, newRepetitions, DateTimeOffset.UtcNow.AddDays(newInterval));
    }
}
