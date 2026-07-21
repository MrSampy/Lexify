using Lexify.Application.Tests.Services;

namespace Lexify.Application.Tests.Tests;

public class DefinitionValidatorTests
{
    private const string Term = "dog";
    private const string Translation = "собака";

    [Fact]
    public void Check_ValidDefinition_ReturnsTrimmedDefinition()
    {
        const string definition = "  A loyal four-legged animal often kept as a pet.  ";

        var check = DefinitionValidator.Check(definition, Term, Translation);

        Assert.True(check.IsValid);
        Assert.Equal(definition.Trim(), check.Definition);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Check_EmptyDefinition_Fails(string? definition)
    {
        Assert.False(DefinitionValidator.Check(definition, Term, Translation).IsValid);
    }

    [Fact]
    public void Check_TooShort_Fails()
    {
        Assert.False(DefinitionValidator.Check("A pet animal.", Term, Translation).IsValid);
    }

    [Fact]
    public void Check_TooManyWords_Fails()
    {
        var definition = string.Join(' ', Enumerable.Repeat("word", 31)) + ".";
        Assert.False(DefinitionValidator.Check(definition, Term, Translation).IsValid);
    }

    [Fact]
    public void Check_ContainsTerm_Fails()
    {
        var check = DefinitionValidator.Check(
            "A dog is a loyal domesticated animal kept as a pet.", Term, Translation);

        Assert.False(check.IsValid);
        Assert.Contains(Term, check.ErrorMessage!);
    }

    [Fact]
    public void Check_ContainsTermCaseInsensitive_Fails()
    {
        Assert.False(DefinitionValidator.Check(
            "A Dog is a loyal domesticated animal kept as a pet.", Term, Translation).IsValid);
    }

    [Fact]
    public void Check_ContainsTranslation_Fails()
    {
        Assert.False(DefinitionValidator.Check(
            "A loyal animal, known as собака, often kept as a pet.", Term, Translation).IsValid);
    }

    [Fact]
    public void Check_TermInsideAnotherWord_IsAllowed()
    {
        // "dogma" contains "dog" but not as a whole word — the word-boundary regex must not flag it.
        var check = DefinitionValidator.Check(
            "A dogmatic belief has nothing to do with this loyal animal.", Term, Translation);

        Assert.True(check.IsValid);
    }
}
