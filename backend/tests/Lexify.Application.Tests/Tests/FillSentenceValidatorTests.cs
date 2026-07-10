using Lexify.Application.Tests.Services;

namespace Lexify.Application.Tests.Tests;

public class FillSentenceValidatorTests
{
    private const string ValidSentence = "The little dog barked loudly at the passing car.";

    [Fact]
    public void Check_ValidSentence_Passes()
    {
        var result = FillSentenceValidator.Check(ValidSentence, "dog");

        Assert.True(result.IsValid);
        Assert.Equal(ValidSentence, result.Sentence);
    }

    [Fact]
    public void Check_SentenceMissingTerm_Fails()
    {
        var result = FillSentenceValidator.Check("The cat sat on the mat quietly today.", "dog");

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Check_TermUsedTwice_Fails()
    {
        var result = FillSentenceValidator.Check("The dog chased another dog around the park today.", "dog");

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Check_TermMatchIsCaseInsensitive()
    {
        var result = FillSentenceValidator.Check("Dog owners walk their pets every single morning.", "dog");

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Check_SentenceTooShortInWords_Fails()
    {
        var result = FillSentenceValidator.Check("A dog barks.", "dog");

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Check_SentenceTooLongInWords_Fails()
    {
        var words = string.Join(' ', Enumerable.Repeat("word", 25).Append("dog"));
        var result = FillSentenceValidator.Check(words + ".", "dog");

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Check_SentenceWithoutTerminalPunctuation_Fails()
    {
        var result = FillSentenceValidator.Check(ValidSentence.TrimEnd('.'), "dog");

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Check_EmptySentence_Fails()
    {
        var result = FillSentenceValidator.Check("", "dog");

        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void Check_NullSentence_Fails()
    {
        var result = FillSentenceValidator.Check(null, "dog");

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Check_DoesNotMatchTermAsSubstringOfAnotherWord()
    {
        // "doggy" contains "dog" but is a different word — must not count as an occurrence.
        var result = FillSentenceValidator.Check("The doggy toy squeaked loudly on the wooden floor.", "dog");

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Blank_ReplacesTheSingleOccurrenceWithPlaceholder()
    {
        var blanked = FillSentenceValidator.Blank(ValidSentence, "dog");

        Assert.DoesNotContain("dog", blanked, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("___", blanked);
    }

    [Fact]
    public void Blank_PreservesTheRestOfTheSentence()
    {
        var blanked = FillSentenceValidator.Blank(ValidSentence, "dog");

        Assert.Equal("The little ___ barked loudly at the passing car.", blanked);
    }
}
