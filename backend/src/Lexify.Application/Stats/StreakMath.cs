namespace Lexify.Application.Stats;

/// <summary>
/// Pure streak arithmetic over the set of UTC dates a user was active (reviewed at least one word).
/// Kept separate from the query handlers so it can be unit-tested without a database.
/// </summary>
public static class StreakMath
{
    /// <summary>
    /// Consecutive active days ending today. If today has no activity yet the streak is measured from
    /// yesterday (a one-day grace so an unfinished day doesn't read as a broken streak); it only drops
    /// to zero once a full day passes with no review.
    /// </summary>
    public static int CurrentStreak(IReadOnlySet<DateOnly> activeDates, DateOnly today)
    {
        var cursor = activeDates.Contains(today) ? today : today.AddDays(-1);
        var streak = 0;
        while (activeDates.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }
        return streak;
    }

    /// <summary>Longest run of consecutive active days anywhere in the set.</summary>
    public static int LongestStreak(IReadOnlySet<DateOnly> activeDates)
    {
        if (activeDates.Count == 0) return 0;

        var ordered = activeDates.OrderBy(d => d).ToList();
        var longest = 1;
        var run = 1;
        for (var i = 1; i < ordered.Count; i++)
        {
            run = ordered[i] == ordered[i - 1].AddDays(1) ? run + 1 : 1;
            if (run > longest) longest = run;
        }
        return longest;
    }
}
