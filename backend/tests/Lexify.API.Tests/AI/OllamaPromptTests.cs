using Lexify.Infrastructure.AI;

namespace Lexify.API.Tests.AI;

public class OllamaPromptTests
{
    [Fact]
    public void EnrichSystemPrompt_DoesNotUseQwen3ThinkingDirective()
    {
        // /no_think is a Qwen3-only control token; Qwen3-*-Instruct-2507 variants are non-thinking
        // by default and have no mode to disable, so the prompt must not reference it.
        var prompt = OpenAiCompatibleClient.BuildEnrichSystemPrompt("English", "Ukrainian");

        Assert.DoesNotContain("think", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnrichSystemPrompt_MentionsTargetAndNativeLanguage()
    {
        var prompt = OpenAiCompatibleClient.BuildEnrichSystemPrompt("English", "Ukrainian");

        Assert.Contains("English", prompt);
        Assert.Contains("Ukrainian", prompt);
    }

    [Fact]
    public void EnrichSystemPrompt_InstructsToPreserveParsedTermAndTranslationExactly()
    {
        var prompt = OpenAiCompatibleClient.BuildEnrichSystemPrompt("English", "Ukrainian");

        Assert.Contains("EXACTLY", prompt);
    }

    [Fact]
    public void EnrichSystemPrompt_RequiresEchoingTheSameIdValues()
    {
        var prompt = OpenAiCompatibleClient.BuildEnrichSystemPrompt("English", "Ukrainian");

        Assert.Contains("SAME \"id\" values", prompt);
    }
}
