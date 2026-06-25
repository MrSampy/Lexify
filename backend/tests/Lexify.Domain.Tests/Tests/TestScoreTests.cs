using Lexify.Domain.Common;
using Lexify.Domain.ValueObjects;

namespace Lexify.Domain.Tests.Tests;

public class TestScoreTests
{
    [Fact]
    public void From_ThreeOfFive_CorrectValues()
    {
        var score = TestScore.From(3, 5);

        Assert.Equal(0.6, score.Value, precision: 4);
        Assert.Equal(60.0, score.Percentage, precision: 4);
        Assert.True(score.Passed);
        Assert.Equal(3, score.Correct);
        Assert.Equal(5, score.Total);
    }

    [Fact]
    public void From_ZeroOfFive_ZeroScore()
    {
        var score = TestScore.From(0, 5);

        Assert.Equal(0.0, score.Value);
        Assert.False(score.Passed);
    }

    [Fact]
    public void From_FiveOfFive_PerfectScore()
    {
        var score = TestScore.From(5, 5);

        Assert.Equal(1.0, score.Value);
        Assert.Equal(100.0, score.Percentage);
        Assert.True(score.Passed);
    }

    [Fact]
    public void From_ZeroTotal_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => TestScore.From(0, 0));
    }

    [Fact]
    public void From_NegativeCorrect_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => TestScore.From(-1, 5));
    }

    [Fact]
    public void From_CorrectExceedsTotal_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => TestScore.From(6, 5));
    }

    [Fact]
    public void From_ThreeOfFive_PassedIsTrue_BecauseExactly60Percent()
    {
        var score = TestScore.From(3, 5);

        Assert.True(score.Passed); // >= 0.6 threshold
    }

    [Fact]
    public void From_TwoOfFive_PassedIsFalse()
    {
        var score = TestScore.From(2, 5);

        Assert.False(score.Passed); // 0.4 < 0.6
    }
}
