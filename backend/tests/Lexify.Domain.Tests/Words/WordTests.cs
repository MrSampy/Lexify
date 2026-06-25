using Lexify.Domain.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Events;

namespace Lexify.Domain.Tests.Words;

public class WordTests
{
    private static readonly Guid ValidBlockId = Guid.NewGuid();

    [Fact]
    public void Create_WithEmptyBlockId_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() =>
            Word.Create(Guid.Empty, "apple", "яблуко"));

        Assert.Equal("Block ID cannot be empty.", ex.Message);
    }

    [Fact]
    public void Create_WithEmptyTerm_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() =>
            Word.Create(ValidBlockId, "   ", "яблуко"));

        Assert.Equal("Term cannot be empty.", ex.Message);
    }

    [Fact]
    public void Create_WithEmptyTranslation_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() =>
            Word.Create(ValidBlockId, "apple", ""));

        Assert.Equal("Translation cannot be empty.", ex.Message);
    }

    [Fact]
    public void Create_WithInvalidWordType_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() =>
            Word.Create(ValidBlockId, "apple", "яблуко", "invalid_type"));

        Assert.Contains("Invalid word type", ex.Message);
    }

    [Fact]
    public void Create_WithValidData_HasDefaultSm2State()
    {
        var word = Word.Create(ValidBlockId, "apple", "яблуко");

        Assert.Equal(2.5, word.EaseFactor);
        Assert.Equal(1, word.IntervalDays);
        Assert.Equal(0, word.Repetitions);
    }

    [Fact]
    public void ApplyReviewResult_Quality5_IncreasesEaseFactorAndSetsFirstInterval()
    {
        var word = Word.Create(ValidBlockId, "apple", "яблуко");

        word.ApplyReviewResult(5);

        Assert.Equal(2.6, word.EaseFactor, precision: 5);
        Assert.Equal(1, word.Repetitions);
        Assert.Equal(1, word.IntervalDays);
    }

    [Fact]
    public void ApplyReviewResult_Quality3_DecreasesEaseFactorSlightly()
    {
        var word = Word.Create(ValidBlockId, "apple", "яблуко");

        // newEF = 2.5 + (0.1 - (5-3)*(0.08+(5-3)*0.02)) = 2.5 - 0.14 = 2.36
        word.ApplyReviewResult(3);

        Assert.Equal(2.36, word.EaseFactor, precision: 5);
    }

    [Fact]
    public void ApplyReviewResult_Quality0_ResetsRepetitionsAndInterval()
    {
        var word = Word.Create(ValidBlockId, "apple", "яблуко");
        word.ApplyReviewResult(5);
        word.ClearDomainEvents();

        word.ApplyReviewResult(0);

        Assert.Equal(0, word.Repetitions);
        Assert.Equal(1, word.IntervalDays);
    }

    [Fact]
    public void ApplyReviewResult_EaseFactorFloor_NeverBelowOnePointThree()
    {
        var word = Word.Create(ValidBlockId, "apple", "яблуко");
        // EF=2.5; each quality=0 reduces EF by 0.8 → 1.7 → 0.9 clamped to 1.3 → stays 1.3
        word.ApplyReviewResult(0);
        word.ApplyReviewResult(0);
        word.ApplyReviewResult(0);

        Assert.Equal(1.3, word.EaseFactor, precision: 5);
    }

    [Fact]
    public void ApplyReviewResult_RaisesWordReviewedEvent()
    {
        var word = Word.Create(ValidBlockId, "apple", "яблуко");

        word.ApplyReviewResult(5);

        var evt = Assert.Single(word.DomainEvents);
        Assert.IsType<WordReviewedEvent>(evt);
    }
}
