namespace Lexify.Application.Conversations.Common;

/// <summary>Final game score for a "Talk to Lexi" session, shown on the end screen.</summary>
public sealed record ConversationScore(
    int Points,
    int Stars,
    int WordsUsed,
    int TotalWords,
    int MessagesUsed,
    int MessageBudget);

/// <summary>
/// Deterministic word-usage detection and challenge scoring for conversations. Kept out of the handler
/// so the same normalization the client uses for its live chips (lowercase + strip punctuation, substring
/// match) is the single source of truth server-side too — the LLM is only a secondary signal for
/// correctness, never for whether a word was used.
/// </summary>
public static class ConversationScoring
{
    /// <summary>Extra messages allowed beyond one-per-target-word.</summary>
    public const int BudgetSlack = 2;
    private const int MinBudget = 3;
    private const int PointsPerWord = 10;
    private const int ComboBonusPerExtraWord = 5;

    public static int BudgetFor(int targetWordCount) => Math.Max(MinBudget, targetWordCount + BudgetSlack);

    /// <summary>Lowercase and replace every non-alphanumeric char with a space, so punctuation never blocks a match.</summary>
    public static string Normalize(string text) =>
        new(text.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) ? c : ' ').ToArray());

    /// <summary>
    /// Whether <paramref name="term"/> occurs in the already-normalized learner text. Matching is at
    /// word boundaries — a plain substring check let "cat" pass inside "category", and this feeds SM-2.
    /// A term token still matches an inflected token that extends it by up to <see cref="MaxInflectionSuffix"/>
    /// chars ("embark" → "embarked"), except for very short terms where that would match everything.
    /// </summary>
    public static bool IsTermUsed(string normalizedLearnerText, string term) =>
        ContainsTerm(Tokenize(normalizedLearnerText), Tokenize(Normalize(term)));

    /// <summary>Shortest term token allowed to match as a prefix of a longer (inflected) token.</summary>
    private const int MinPrefixMatchLength = 4;
    /// <summary>How many extra chars an inflected token may add ("-s", "-ed", "-ing").</summary>
    private const int MaxInflectionSuffix = 3;

    private static string[] Tokenize(string normalizedText) =>
        normalizedText.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static bool ContainsTerm(string[] textTokens, string[] termTokens)
    {
        if (termTokens.Length == 0 || textTokens.Length < termTokens.Length) return false;

        for (var start = 0; start <= textTokens.Length - termTokens.Length; start++)
        {
            var all = true;
            for (var i = 0; i < termTokens.Length; i++)
            {
                if (!TokenMatches(textTokens[start + i], termTokens[i])) { all = false; break; }
            }
            if (all) return true;
        }
        return false;
    }

    private static bool TokenMatches(string textToken, string termToken) =>
        textToken.Equals(termToken, StringComparison.Ordinal) ||
        (termToken.Length >= MinPrefixMatchLength &&
         textToken.Length - termToken.Length is > 0 and <= MaxInflectionSuffix &&
         textToken.StartsWith(termToken, StringComparison.Ordinal));

    public static ConversationScore Compute(
        IReadOnlyList<string> terms,
        IReadOnlyList<string> learnerMessages,
        int finalUsedCount)
    {
        var budget = BudgetFor(terms.Count);
        var points = finalUsedCount * PointsPerWord + ComboBonus(terms, learnerMessages);
        var stars = Stars(finalUsedCount, terms.Count, learnerMessages.Count, budget);
        return new ConversationScore(points, stars, finalUsedCount, terms.Count, learnerMessages.Count, budget);
    }

    /// <summary>Bonus for cramming several target words into one message: (k-1)*5 the first time a message introduces k≥2 new terms.</summary>
    private static int ComboBonus(IReadOnlyList<string> terms, IReadOnlyList<string> learnerMessages)
    {
        var normTerms = terms.Select(t => Tokenize(Normalize(t))).ToList();
        var seen = new bool[normTerms.Count];
        var bonus = 0;

        foreach (var message in learnerMessages)
        {
            var textTokens = Tokenize(Normalize(message));
            var newInMessage = 0;
            for (var i = 0; i < normTerms.Count; i++)
            {
                if (seen[i]) continue;
                if (ContainsTerm(textTokens, normTerms[i]))
                {
                    seen[i] = true;
                    newInMessage++;
                }
            }
            if (newInMessage >= 2) bonus += (newInMessage - 1) * ComboBonusPerExtraWord;
        }

        return bonus;
    }

    private static int Stars(int used, int total, int messages, int budget)
    {
        if (total == 0 || used == 0) return 0;
        if (used == total && messages <= budget) return 3;
        if (used == total || (messages <= budget && used * 2 >= total)) return 2;
        return 1;
    }
}
