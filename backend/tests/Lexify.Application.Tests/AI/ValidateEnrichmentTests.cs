using Lexify.Application.AI;
using Lexify.Application.AI.Dtos;

namespace Lexify.Application.Tests.AI;

public class ValidateEnrichmentTests
{
    private static ParsedImportLine Parsed(int id, string term, string translation, bool confidenceFlag = false) =>
        new(id, $"{term} - {translation}", term, translation, [], confidenceFlag);

    private static ParsedImportLine Raw(int id, string rawLine) =>
        new(id, rawLine, null, null, [], false);

    [Fact]
    public void ValidateEnrichment_AllIdsPresent_ReturnsOkWithEnrichedFields()
    {
        var batch = new[] { Parsed(1, "dog", "собака") };
        const string json = """
            {"suggestedTitle":"Animals","words":[
                {"id":1,"term":"dog","translation":"собака","wordType":"word",
                 "alternativeTranslations":[],"notes":"noun","exampleSentence":"The dog barks.","confidenceNote":null}
            ]}
            """;

        var result = AIResponseValidator.ValidateEnrichment(json, batch);

        Assert.True(result.IsValid);
        Assert.Equal("Animals", result.ParsedResult!.SuggestedTitle);
        var word = result.ParsedResult.Words[0];
        Assert.Equal("word", word.WordType);
        Assert.Equal("noun", word.Notes);
        Assert.Equal("The dog barks.", word.ExampleSentence);
    }

    [Fact]
    public void ValidateEnrichment_ParsedLine_RepairsTermAndTranslationInsteadOfTrustingAi()
    {
        // The model was told to echo term/translation exactly but altered them anyway — the
        // deterministic parser output must win, not be treated as a validation failure.
        var batch = new[] { Parsed(1, "dog", "собака") };
        const string json = """
            {"suggestedTitle":null,"words":[
                {"id":1,"term":"doggo","translation":"песик","wordType":"word",
                 "alternativeTranslations":[],"notes":null,"exampleSentence":null,"confidenceNote":null}
            ]}
            """;

        var result = AIResponseValidator.ValidateEnrichment(json, batch);

        Assert.True(result.IsValid);
        Assert.Equal("dog", result.ParsedResult!.Words[0].Term);
        Assert.Equal("собака", result.ParsedResult.Words[0].Translation);
    }

    [Fact]
    public void ValidateEnrichment_RawLine_UsesAiExtractedTermAndTranslation()
    {
        var batch = new[] { Raw(1, "some unparseable line") };
        const string json = """
            {"suggestedTitle":null,"words":[
                {"id":1,"term":"line","translation":"рядок","wordType":"word",
                 "alternativeTranslations":[],"notes":null,"exampleSentence":null,"confidenceNote":null}
            ]}
            """;

        var result = AIResponseValidator.ValidateEnrichment(json, batch);

        Assert.True(result.IsValid);
        Assert.Equal("line", result.ParsedResult!.Words[0].Term);
        Assert.Equal("рядок", result.ParsedResult.Words[0].Translation);
    }

    [Fact]
    public void ValidateEnrichment_MissingId_Fails()
    {
        var batch = new[] { Parsed(1, "dog", "собака"), Parsed(2, "cat", "кіт") };
        const string json = """
            {"suggestedTitle":null,"words":[
                {"id":1,"term":"dog","translation":"собака","wordType":"word",
                 "alternativeTranslations":[],"notes":null,"exampleSentence":null,"confidenceNote":null}
            ]}
            """;

        var result = AIResponseValidator.ValidateEnrichment(json, batch);

        Assert.False(result.IsValid);
        Assert.Contains("2", result.ErrorMessage);
    }

    [Fact]
    public void ValidateEnrichment_MalformedJson_Fails()
    {
        var batch = new[] { Parsed(1, "dog", "собака") };

        var result = AIResponseValidator.ValidateEnrichment("not json at all", batch);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateEnrichment_EmptyResponse_Fails()
    {
        var result = AIResponseValidator.ValidateEnrichment("", [Parsed(1, "dog", "собака")]);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateEnrichment_MergesAndDeduplicatesAlternativeTranslations()
    {
        var batch = new[]
        {
            new ParsedImportLine(1, "dog - собака, пес", "dog", "собака", ["пес"], false)
        };
        const string json = """
            {"suggestedTitle":null,"words":[
                {"id":1,"term":"dog","translation":"собака","wordType":"word",
                 "alternativeTranslations":["пес","песик"],"notes":null,"exampleSentence":null,"confidenceNote":null}
            ]}
            """;

        var result = AIResponseValidator.ValidateEnrichment(json, batch);

        Assert.True(result.IsValid);
        Assert.Equal(["пес", "песик"], result.ParsedResult!.Words[0].AlternativeTranslations);
    }

    [Fact]
    public void ValidateEnrichment_ConfidenceFlagComesFromParserNotAi()
    {
        var batch = new[] { Parsed(1, "erode", "підривати", confidenceFlag: true) };
        const string json = """
            {"suggestedTitle":null,"words":[
                {"id":1,"term":"erode","translation":"підривати","wordType":"word",
                 "alternativeTranslations":[],"notes":null,"exampleSentence":null,"confidenceNote":"ambiguous"}
            ]}
            """;

        var result = AIResponseValidator.ValidateEnrichment(json, batch);

        Assert.True(result.ParsedResult!.Words[0].ConfidenceFlag);
        Assert.Equal("ambiguous", result.ParsedResult.Words[0].ConfidenceNote);
    }

    [Fact]
    public void DegradeToParsedOnly_KeepsOnlyParsedLinesWithDefaultEnrichment()
    {
        var batch = new[]
        {
            Parsed(1, "dog", "собака", confidenceFlag: true),
            Raw(2, "unparseable garbage")
        };

        var result = AIResponseValidator.DegradeToParsedOnly(batch);

        Assert.Single(result.Words);
        Assert.Equal("dog", result.Words[0].Term);
        Assert.Equal("word", result.Words[0].WordType);
        Assert.True(result.Words[0].ConfidenceFlag);
        Assert.Null(result.Words[0].ExampleSentence);
    }
}
