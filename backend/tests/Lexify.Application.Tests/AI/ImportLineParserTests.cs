using Lexify.Application.AI;

namespace Lexify.Application.Tests.AI;

public class ImportLineParserTests
{
    [Fact]
    public void Parse_DashSeparator_SplitsTermAndTranslation()
    {
        var result = ImportLineParser.Parse("dog - собака");

        Assert.Single(result);
        Assert.True(result[0].IsParsed);
        Assert.Equal("dog", result[0].Term);
        Assert.Equal("собака", result[0].Translation);
    }

    [Theory]
    [InlineData("dog — собака")]
    [InlineData("dog – собака")]
    public void Parse_DashVariantSeparators_SplitTermAndTranslation(string line)
    {
        var result = ImportLineParser.Parse(line);

        Assert.True(result[0].IsParsed);
        Assert.Equal("dog", result[0].Term);
        Assert.Equal("собака", result[0].Translation);
    }

    [Fact]
    public void Parse_TabSeparator_SplitsTermAndTranslation()
    {
        var result = ImportLineParser.Parse("dog\tсобака");

        Assert.True(result[0].IsParsed);
        Assert.Equal("dog", result[0].Term);
        Assert.Equal("собака", result[0].Translation);
    }

    [Fact]
    public void Parse_ColonSeparator_SplitsTermAndTranslation()
    {
        var result = ImportLineParser.Parse("dog: собака");

        Assert.True(result[0].IsParsed);
        Assert.Equal("dog", result[0].Term);
        Assert.Equal("собака", result[0].Translation);
    }

    [Fact]
    public void Parse_TrailingQuestionMark_SetsConfidenceFlagAndStripsIt()
    {
        var result = ImportLineParser.Parse("erode - підривати?");

        Assert.True(result[0].ConfidenceFlag);
        Assert.Equal("підривати", result[0].Translation);
    }

    [Fact]
    public void Parse_NoQuestionMark_ConfidenceFlagIsFalse()
    {
        var result = ImportLineParser.Parse("dog - собака");

        Assert.False(result[0].ConfidenceFlag);
    }

    [Fact]
    public void Parse_CommaSeparatedTranslations_FirstIsPrimaryRestAreAlternatives()
    {
        var result = ImportLineParser.Parse("perro - собака, пёс");

        Assert.Equal("собака", result[0].Translation);
        Assert.Equal(["пёс"], result[0].AlternativeTranslations);
    }

    [Fact]
    public void Parse_SlashSeparatedTranslations_FirstIsPrimaryRestAreAlternatives()
    {
        var result = ImportLineParser.Parse("chuffed - задоволений/радий");

        Assert.Equal("задоволений", result[0].Translation);
        Assert.Equal(["радий"], result[0].AlternativeTranslations);
    }

    [Fact]
    public void Parse_SingleTranslation_HasNoAlternatives()
    {
        var result = ImportLineParser.Parse("dog - собака");

        Assert.Empty(result[0].AlternativeTranslations);
    }

    [Fact]
    public void Parse_LineWithoutSeparator_IsUnparsedAndKeepsRawLine()
    {
        var result = ImportLineParser.Parse("just some garbage text");

        Assert.False(result[0].IsParsed);
        Assert.Null(result[0].Term);
        Assert.Null(result[0].Translation);
        Assert.Equal("just some garbage text", result[0].RawLine);
    }

    [Fact]
    public void Parse_SeparatorWithEmptySide_IsUnparsed()
    {
        var result = ImportLineParser.Parse("dog - ");

        Assert.False(result[0].IsParsed);
    }

    [Fact]
    public void Parse_MultipleLines_AssignsSequentialOneBasedIds()
    {
        var result = ImportLineParser.Parse("dog - собака\ncat - кіт\nunparseable line");

        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal(2, result[1].Id);
        Assert.Equal(3, result[2].Id);
    }

    [Fact]
    public void Parse_BlankLinesAreSkipped()
    {
        var result = ImportLineParser.Parse("dog - собака\n\n\ncat - кіт");

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Parse_TrimsWhitespaceAroundTermAndTranslation()
    {
        var result = ImportLineParser.Parse("  dog   -   собака  ");

        Assert.Equal("dog", result[0].Term);
        Assert.Equal("собака", result[0].Translation);
    }

    [Fact]
    public void Parse_EmptyInput_ReturnsEmptyList()
    {
        Assert.Empty(ImportLineParser.Parse(""));
    }
}
