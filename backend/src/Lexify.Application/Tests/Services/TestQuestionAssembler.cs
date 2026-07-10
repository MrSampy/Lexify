using Lexify.Domain.Entities;

namespace Lexify.Application.Tests.Services;

/// <summary>
/// Builds quiz questions entirely in code from a Word and a pre-fetched distractor list — no LLM
/// call needed for the question's structure, text, or options. Callers (GenerateTestJob) resolve
/// distractors via DistractorPool (+ IAIProvider.GenerateFakeDistractorsAsync as a last resort)
/// before calling these methods.
/// </summary>
public static class TestQuestionAssembler
{
    public static AssembledQuestion TranslateToNative(Word word, IReadOnlyList<string> distractors)
    {
        var options = BuildOptions(word.Translation, distractors, HashCode.Combine(word.Id, Question.QuestionTypes.TranslateToNative));
        return new AssembledQuestion(
            Question.QuestionTypes.TranslateToNative,
            QuestionTemplates.TranslateToNative(word.Term),
            word.Translation,
            word.Id,
            options);
    }

    public static AssembledQuestion TranslateToForeign(Word word, string targetLanguageName, IReadOnlyList<string> distractors)
    {
        var options = BuildOptions(word.Term, distractors, HashCode.Combine(word.Id, Question.QuestionTypes.TranslateToForeign));
        return new AssembledQuestion(
            Question.QuestionTypes.TranslateToForeign,
            QuestionTemplates.TranslateToForeign(word.Translation, targetLanguageName),
            word.Term,
            word.Id,
            options);
    }

    /// <summary>
    /// Correct set = primary translation + up to 2 alternative translations, padded with
    /// distractors. Grading (SubmitAnswerCommandHandler) reads Options.IsCorrect, not CorrectAnswer —
    /// the joined string here is display-only, but must list every correct option (not just the
    /// first) so a user who picked one of several correct answers doesn't see a misleading result
    /// next to the exact text they chose.
    /// </summary>
    public static AssembledQuestion MultiSelectTheme(Word word, IReadOnlyList<string> distractors)
    {
        var correctAnswers = new List<string> { word.Translation };
        correctAnswers.AddRange(word.AlternativeTranslations.Take(2));

        var rng = new Random(HashCode.Combine(word.Id, Question.QuestionTypes.MultiSelectTheme));
        var chosenDistractors = distractors
            .Where(d => !correctAnswers.Contains(d, StringComparer.OrdinalIgnoreCase))
            .OrderBy(_ => rng.Next())
            .ToList();

        var options = correctAnswers.Select(a => new AssembledOption(a, true))
            .Concat(chosenDistractors.Select(d => new AssembledOption(d, false)))
            .OrderBy(_ => rng.Next())
            .ToList();

        return new AssembledQuestion(
            Question.QuestionTypes.MultiSelectTheme,
            QuestionTemplates.MultiSelectTheme(word.Term),
            string.Join(", ", correctAnswers),
            word.Id,
            options);
    }

    public static AssembledQuestion OpenAnswer(Word word)
    {
        // CorrectAnswer lists every accepted variant — SubmitAnswerCommandHandler's fuzzy check
        // already splits on '/' and ',' and accepts any match, so this makes alternative
        // translations gradeable without touching that grading logic.
        var acceptedAnswers = new[] { word.Translation }.Concat(word.AlternativeTranslations);
        return new AssembledQuestion(
            Question.QuestionTypes.OpenAnswer,
            QuestionTemplates.OpenAnswer(word.Term),
            string.Join(", ", acceptedAnswers),
            word.Id,
            []);
    }

    public static AssembledQuestion FillInSentence(Word word, string blankedSentence, IReadOnlyList<string> distractors)
    {
        var options = BuildOptions(word.Term, distractors, HashCode.Combine(word.Id, Question.QuestionTypes.FillInSentence));
        return new AssembledQuestion(
            Question.QuestionTypes.FillInSentence,
            blankedSentence,
            word.Term,
            word.Id,
            options);
    }

    private static List<AssembledOption> BuildOptions(string correctText, IReadOnlyList<string> distractors, int seed)
    {
        var rng = new Random(seed);
        var options = new List<AssembledOption> { new(correctText, true) };
        options.AddRange(distractors
            .Where(d => !string.Equals(d, correctText, StringComparison.OrdinalIgnoreCase))
            .Take(3)
            .Select(d => new AssembledOption(d, false)));

        return options.OrderBy(_ => rng.Next()).ToList();
    }
}

public sealed record AssembledQuestion(
    string QuestionType,
    string QuestionText,
    string CorrectAnswer,
    Guid? WordId,
    IReadOnlyList<AssembledOption> Options);

public sealed record AssembledOption(string Text, bool IsCorrect);
