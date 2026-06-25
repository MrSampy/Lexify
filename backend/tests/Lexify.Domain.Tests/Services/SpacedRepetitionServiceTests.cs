using Lexify.Domain.Common;
using Lexify.Domain.Services;

namespace Lexify.Domain.Tests.Services;

public class SpacedRepetitionServiceTests
{
    [Fact]
    public void Calculate_Quality5_IncreasesEaseFactor()
    {
        // newEF = 2.5 + (0.1 - 0*...) = 2.6
        var result = SpacedRepetitionService.Calculate(2.5, 1, 0, 5);

        Assert.Equal(2.6, result.EaseFactor, precision: 5);
    }

    [Fact]
    public void Calculate_Quality3_SlightlyDecreasesEaseFactor()
    {
        // newEF = 2.5 + (0.1 - 2*(0.08 + 2*0.02)) = 2.5 - 0.14 = 2.36
        var result = SpacedRepetitionService.Calculate(2.5, 1, 0, 3);

        Assert.Equal(2.36, result.EaseFactor, precision: 5);
    }

    [Fact]
    public void Calculate_Quality0_EaseFactorDecreases()
    {
        // newEF = 2.5 + (0.1 - 5*(0.08 + 5*0.02)) = 2.5 - 0.8 = 1.7
        var result = SpacedRepetitionService.Calculate(2.5, 1, 0, 0);

        Assert.Equal(1.7, result.EaseFactor, precision: 5);
    }

    [Fact]
    public void Calculate_Quality0_ResetsIntervalAndRepetitions()
    {
        var result = SpacedRepetitionService.Calculate(2.5, 6, 2, 0);

        Assert.Equal(1, result.IntervalDays);
        Assert.Equal(0, result.Repetitions);
    }

    [Fact]
    public void Calculate_Quality0_EaseFactorFloorEnforced()
    {
        // EF=1.3, quality=0: 1.3 - 0.8 = 0.5 → clamped to 1.3
        var result = SpacedRepetitionService.Calculate(1.3, 1, 0, 0);

        Assert.Equal(1.3, result.EaseFactor, precision: 5);
    }

    [Fact]
    public void Calculate_FirstRepetition_IntervalIsOne()
    {
        var result = SpacedRepetitionService.Calculate(2.5, 1, 0, 5);

        Assert.Equal(1, result.IntervalDays);
        Assert.Equal(1, result.Repetitions);
    }

    [Fact]
    public void Calculate_SecondRepetition_IntervalIsSix()
    {
        var result = SpacedRepetitionService.Calculate(2.5, 1, 1, 5);

        Assert.Equal(6, result.IntervalDays);
        Assert.Equal(2, result.Repetitions);
    }

    [Fact]
    public void Calculate_ThirdRepetition_IntervalIsEFBased()
    {
        // reps=2, intervalDays=6, EF=2.5, quality=5 → newEF=2.6, interval=round(6*2.6)=16
        var result = SpacedRepetitionService.Calculate(2.5, 6, 2, 5);

        Assert.Equal(16, result.IntervalDays);
        Assert.Equal(3, result.Repetitions);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(-1)]
    public void Calculate_InvalidQuality_ThrowsDomainException(int quality)
    {
        Assert.Throws<DomainException>(() =>
            SpacedRepetitionService.Calculate(2.5, 1, 0, quality));
    }

    [Fact]
    public void Calculate_NextReviewAt_IsInTheFuture()
    {
        var before = DateTimeOffset.UtcNow;
        var result = SpacedRepetitionService.Calculate(2.5, 1, 0, 5);

        Assert.True(result.NextReviewAt >= before.AddDays(1).AddSeconds(-1));
    }
}
