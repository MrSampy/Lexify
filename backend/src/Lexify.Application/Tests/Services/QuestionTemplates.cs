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
}
