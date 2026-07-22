using Lexify.Application.Conversations.Common;

namespace Lexify.Application.Tests.Conversations;

public class ConversationScoringTests
{
    [Theory]
    [InlineData(0, 3)]   // min budget floor
    [InlineData(1, 3)]
    [InlineData(6, 8)]   // words + slack
    public void BudgetFor_AppliesSlackAndFloor(int words, int expected)
        => Assert.Equal(expected, ConversationScoring.BudgetFor(words));

    [Fact]
    public void IsTermUsed_IgnoresCaseAndPunctuation()
    {
        var text = ConversationScoring.Normalize("Today I will EMBARK, on a trip!");
        Assert.True(ConversationScoring.IsTermUsed(text, "embark"));
        Assert.False(ConversationScoring.IsTermUsed(text, "surge"));
    }

    [Fact]
    public void Compute_AllWordsWithinBudget_ThreeStars()
    {
        var terms = new[] { "nap", "doze", "snore" };
        var messages = new[] { "I nap and doze then snore." }; // 1 message, budget = 5

        var score = ConversationScoring.Compute(terms, messages, finalUsedCount: 3);

        Assert.Equal(3, score.Stars);
        // 3 words * 10 + combo (3 new in one message → (3-1)*5 = 10) = 40
        Assert.Equal(40, score.Points);
        Assert.Equal(1, score.MessagesUsed);
        Assert.Equal(5, score.MessageBudget);
    }

    [Fact]
    public void Compute_NoWords_ZeroStars()
    {
        var score = ConversationScoring.Compute(["nap", "doze"], ["hello there"], finalUsedCount: 0);
        Assert.Equal(0, score.Stars);
        Assert.Equal(0, score.Points);
    }

    [Fact]
    public void Compute_HalfWordsWithinBudget_TwoStars()
    {
        var terms = new[] { "a", "b", "c", "d" };
        var score = ConversationScoring.Compute(terms, ["a b"], finalUsedCount: 2);
        Assert.Equal(2, score.Stars); // half used, within budget
    }
}
