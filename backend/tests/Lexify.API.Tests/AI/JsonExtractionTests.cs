using Lexify.Application.AI;

namespace Lexify.API.Tests.AI;

/// <summary>
/// AIResponseValidator's balanced-bracket extractors are the shared defense-in-depth layer behind
/// every AI JSON response (enrichment, fill sentences, distractors) — they strip chatty filler text
/// models sometimes add around the actual JSON, independent of response_format enforcement.
/// </summary>
public class JsonExtractionTests
{
    [Fact]
    public void ExtractFirstJsonObject_IgnoresChattyTextAroundIt()
    {
        const string raw = "Sure, here you go:\n{\"a\":1,\"b\":{\"c\":2}}\nHope that helps!";

        var json = AIResponseValidator.ExtractFirstJsonObject(raw);

        Assert.Equal("""{"a":1,"b":{"c":2}}""", json);
    }

    [Fact]
    public void ExtractFirstJsonObject_ReturnsNullWhenNoObjectPresent()
    {
        Assert.Null(AIResponseValidator.ExtractFirstJsonObject("just some text, no braces here"));
    }

    [Fact]
    public void ExtractFirstJsonArray_HandlesNestedBracketsAndStrings()
    {
        const string raw = """prefix [1, [2, 3], {"x": "] not a bracket"}] suffix""";

        var json = AIResponseValidator.ExtractFirstJsonArray(raw);

        Assert.Equal("""[1, [2, 3], {"x": "] not a bracket"}]""", json);
    }

    [Fact]
    public void ExtractFirstJsonArray_ReturnsNullWhenNoArrayPresent()
    {
        Assert.Null(AIResponseValidator.ExtractFirstJsonArray("no brackets in sight"));
    }
}
