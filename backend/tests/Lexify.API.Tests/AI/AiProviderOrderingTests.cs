using Lexify.Infrastructure.AI;
using Lexify.Infrastructure.Settings;

namespace Lexify.API.Tests.AI;

/// <summary>
/// Verifies AiProviderOrdering.Order: interchangeable keys (same BaseUrl) are round-robined by the seed,
/// while distinct endpoints keep their configured fallback order. This is what spreads AI load across
/// the several Ollama Cloud keys without ever promoting a real fallback provider ahead of its primary.
/// </summary>
public class AiProviderOrderingTests
{
    private const string OllamaUrl = "https://ollama.com";
    private const string LemonadeUrl = "http://localhost:13305";

    private static AiProviderSettings P(string name, string baseUrl) =>
        new() { Name = name, BaseUrl = baseUrl, Model = "m", ApiKey = "k" };

    [Theory]
    [InlineData(0, "A,B,C,D")]
    [InlineData(1, "B,C,D,A")]
    [InlineData(2, "C,D,A,B")]
    [InlineData(3, "D,A,B,C")]
    [InlineData(4, "A,B,C,D")] // wraps back around
    public void Order_SingleEndpointPool_RotatesStartBySeed(long seed, string expected)
    {
        var providers = new[]
        {
            P("A", OllamaUrl), P("B", OllamaUrl), P("C", OllamaUrl), P("D", OllamaUrl),
        };

        var result = AiProviderOrdering.Order(providers, seed);

        Assert.Equal(expected, string.Join(",", result.Select(p => p.Name)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Order_EveryRotation_IsAFullPermutationWithNoDuplicates(long seed)
    {
        var providers = new[]
        {
            P("A", OllamaUrl), P("B", OllamaUrl), P("C", OllamaUrl), P("D", OllamaUrl),
        };

        var result = AiProviderOrdering.Order(providers, seed);

        Assert.Equal(4, result.Count);
        Assert.Equal("A,B,C,D", string.Join(",", result.Select(p => p.Name).OrderBy(n => n)));
    }

    [Theory]
    [InlineData(0, "O1,O2,O3")]
    [InlineData(1, "O2,O3,O1")]
    [InlineData(2, "O3,O1,O2")]
    public void Order_MixedEndpoints_RotatesPoolButKeepsFallbackLast(long seed, string expectedOllama)
    {
        // Three interchangeable Ollama keys, then a distinct-endpoint fallback.
        var providers = new[]
        {
            P("O1", OllamaUrl), P("O2", OllamaUrl), P("O3", OllamaUrl), P("Lemonade", LemonadeUrl),
        };

        var result = AiProviderOrdering.Order(providers, seed);

        Assert.Equal($"{expectedOllama},Lemonade", string.Join(",", result.Select(p => p.Name)));
        Assert.Equal("Lemonade", result[^1].Name); // the different endpoint is never rotated ahead
    }

    [Fact]
    public void Order_SingleProvider_ReturnedUnchanged()
    {
        var providers = new[] { P("only", OllamaUrl) };

        var result = AiProviderOrdering.Order(providers, 7);

        Assert.Single(result);
        Assert.Equal("only", result[0].Name);
    }

    [Fact]
    public void Order_EmptyList_ReturnsEmpty()
    {
        Assert.Empty(AiProviderOrdering.Order([], 3));
    }

    [Fact]
    public void Order_LargeSeed_StillProducesValidRotation()
    {
        var providers = new[] { P("A", OllamaUrl), P("B", OllamaUrl), P("C", OllamaUrl) };

        // seed 1_000_000 % 3 == 1 -> starts at B
        var result = AiProviderOrdering.Order(providers, 1_000_000);

        Assert.Equal("B,C,A", string.Join(",", result.Select(p => p.Name)));
    }
}
