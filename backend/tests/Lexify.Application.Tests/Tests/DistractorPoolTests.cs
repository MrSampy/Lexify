using Lexify.Application.Tests.Services;
using Lexify.Domain.Entities;

namespace Lexify.Application.Tests.Tests;

public class DistractorPoolTests
{
    private static Word MakeWord(string term, string translation) =>
        new(Guid.NewGuid(), term, translation);

    [Fact]
    public void TakeTranslations_PrefersCrossBlockWordsBeforeSameBlockWords()
    {
        var target = MakeWord("dog", "собака");
        var crossBlock = new[] { MakeWord("cat", "кіт"), MakeWord("mouse", "миша") };
        var sameBlock = new[] { MakeWord("bird", "птах") };
        var pool = new DistractorPool(crossBlock, sameBlock);

        var result = pool.TakeTranslations(target, 2, new Random(1));

        Assert.Equal(2, result.Count);
        Assert.All(result, t => Assert.Contains(t, crossBlock.Select(w => w.Translation)));
    }

    [Fact]
    public void TakeTranslations_FallsBackToSameBlockWhenCrossBlockIsExhausted()
    {
        var target = MakeWord("dog", "собака");
        var crossBlock = new[] { MakeWord("cat", "кіт") };
        var sameBlock = new[] { MakeWord("bird", "птах") };
        var pool = new DistractorPool(crossBlock, sameBlock);

        var result = pool.TakeTranslations(target, 2, new Random(1));

        Assert.Equal(2, result.Count);
        Assert.Contains("кіт", result);
        Assert.Contains("птах", result);
    }

    [Fact]
    public void TakeTranslations_ExcludesTheTargetWordItself()
    {
        var target = MakeWord("dog", "собака");
        var sameBlock = new[] { target, MakeWord("cat", "кіт") };
        var pool = new DistractorPool([], sameBlock);

        var result = pool.TakeTranslations(target, 5, new Random(1));

        Assert.DoesNotContain("собака", result);
    }

    [Fact]
    public void TakeTranslations_ExcludesTargetsOwnTranslationAndAlternatives()
    {
        var target = MakeWord("dog", "собака");
        target.SetAlternativeTranslations(["пес"]);
        var sameBlock = new[] { MakeWord("puppy", "пес"), MakeWord("cat", "кіт") };
        var pool = new DistractorPool([], sameBlock);

        var result = pool.TakeTranslations(target, 5, new Random(1));

        Assert.DoesNotContain("пес", result);
        Assert.Contains("кіт", result);
    }

    [Fact]
    public void TakeTranslations_NeverReturnsDuplicateValues()
    {
        var target = MakeWord("dog", "собака");
        var sameBlock = new[] { MakeWord("cat1", "кіт"), MakeWord("cat2", "кіт") };
        var pool = new DistractorPool([], sameBlock);

        var result = pool.TakeTranslations(target, 5, new Random(1));

        Assert.Single(result);
    }

    [Fact]
    public void TakeTerms_ExcludesTheTargetsOwnTerm()
    {
        var target = MakeWord("dog", "собака");
        var sameBlock = new[] { target, MakeWord("cat", "кіт") };
        var pool = new DistractorPool([], sameBlock);

        var result = pool.TakeTerms(target, 5, new Random(1));

        Assert.DoesNotContain("dog", result);
        Assert.Contains("cat", result);
    }

    [Fact]
    public void TakeTranslations_ReturnsFewerThanRequestedWhenPoolIsSmall()
    {
        var target = MakeWord("dog", "собака");
        var pool = new DistractorPool([], []);

        var result = pool.TakeTranslations(target, 5, new Random(1));

        Assert.Empty(result);
    }
}
