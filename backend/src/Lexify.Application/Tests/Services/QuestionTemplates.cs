namespace Lexify.Application.Tests.Services;

/// <summary>
/// Question stems for code-assembled questions. English-only, matching what the LLM itself wrote
/// for these question types before this refactor — not a regression, and not yet localized (a
/// template-key-based frontend contract would be needed for real i18n; out of scope here).
/// </summary>
public static class QuestionTemplates
{
    public static string TranslateToNative(string term) =>
        $"What is the translation of '{term}'?";

    public static string TranslateToForeign(string translation, string targetLanguageName) =>
        $"Which {targetLanguageName} word means '{translation}'?";

    public static string MultiSelectTheme(string term) =>
        $"Select all correct translations of '{term}'.";

    public static string OpenAnswer(string term) =>
        $"Translate '{term}'.";

    // The templates below embed word-specific text (terms/translations) on purpose: content hashes
    // are SHA-256(type|text), and both in-test and cross-test dedup need a per-word-unique text.

    public static string MatchingPairs(IEnumerable<string> terms) =>
        $"Match each word to its translation: {string.Join(", ", terms)}.";

    public static string ListenAndType(string translation) =>
        $"Listen and type the word you hear (meaning: '{translation}').";

    public static string WordScramble(string translation) =>
        $"Unscramble the letters to form the word meaning '{translation}'.";

    public static string SentenceBuilder(string translation) =>
        $"Arrange the words into a correct sentence using the word for '{translation}'.";

    public static string DefinitionMatch(string definition) =>
        $"Which word matches this definition? \"{definition}\"";
}
