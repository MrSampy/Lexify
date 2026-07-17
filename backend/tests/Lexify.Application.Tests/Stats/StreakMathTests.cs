using Lexify.Application.Stats;

namespace Lexify.Application.Tests.Stats;

public class StreakMathTests
{
    private static readonly DateOnly Today = new(2026, 7, 16);

    private static HashSet<DateOnly> Dates(params int[] daysAgo) =>
        daysAgo.Select(d => Today.AddDays(-d)).ToHashSet();

    [Fact]
    public void CurrentStreak_ReviewedTodayAndPriorDays_CountsInclusive()
    {
        var active = Dates(0, 1, 2);
        Assert.Equal(3, StreakMath.CurrentStreak(active, Today));
    }

    [Fact]
    public void CurrentStreak_NotReviewedTodayButYesterday_StaysAliveViaGrace()
    {
        var active = Dates(1, 2);
        Assert.Equal(2, StreakMath.CurrentStreak(active, Today));
    }

    [Fact]
    public void CurrentStreak_GapBeforeToday_IsZero()
    {
        // Last activity was two days ago — the grace day (yesterday) is empty, so the streak is broken.
        var active = Dates(2, 3);
        Assert.Equal(0, StreakMath.CurrentStreak(active, Today));
    }

    [Fact]
    public void CurrentStreak_NoActivity_IsZero() =>
        Assert.Equal(0, StreakMath.CurrentStreak(new HashSet<DateOnly>(), Today));

    [Fact]
    public void LongestStreak_PicksLongestRun()
    {
        // Runs: {10,9,8} = 3, {5,4} = 2, {0} = 1  → longest is 3.
        var active = Dates(0, 4, 5, 8, 9, 10);
        Assert.Equal(3, StreakMath.LongestStreak(active));
    }

    [Fact]
    public void LongestStreak_Empty_IsZero() =>
        Assert.Equal(0, StreakMath.LongestStreak(new HashSet<DateOnly>()));
}
