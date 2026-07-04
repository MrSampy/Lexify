using Lexify.Infrastructure.AI;

namespace Lexify.API.Tests.AI;

public class OllamaPromptTests
{
    [Fact]
    public void TestSystemPrompt_UsesNoThinkDirective()
    {
        var prompt = OpenAiCompatibleClient.BuildTestSystemPrompt(5, ["single_choice", "multi_select"]);

        Assert.StartsWith("/no_think", prompt.TrimStart());
        Assert.DoesNotContain("/think", prompt.Replace("/no_think", string.Empty));
    }

    [Fact]
    public void TestSystemPrompt_WithEnglishLevel_MentionsCefrLevel()
    {
        var prompt = OpenAiCompatibleClient.BuildTestSystemPrompt(5, ["single_choice"], "B2");

        Assert.Contains("CEFR B2", prompt);
    }

    [Fact]
    public void TestSystemPrompt_WithoutEnglishLevel_OmitsCefrRule()
    {
        var prompt = OpenAiCompatibleClient.BuildTestSystemPrompt(5, ["single_choice"]);

        Assert.DoesNotContain("CEFR", prompt);
    }

    [Fact]
    public void TestSystemPrompt_ContainsFewShotExamplesForAllQuestionTypes()
    {
        var prompt = OpenAiCompatibleClient.BuildTestSystemPrompt(
            10, ["single_choice", "multi_select", "fill_blank", "open_answer"]);

        Assert.Contains("\"questionType\":\"single_choice\"", prompt);
        Assert.Contains("\"questionType\":\"multi_select\"", prompt);
        Assert.Contains("\"questionType\":\"fill_blank\"", prompt);
        Assert.Contains("\"questionType\":\"open_answer\"", prompt);
        Assert.Contains("___", prompt);
        Assert.Contains("exactly 10", prompt);
    }
}
