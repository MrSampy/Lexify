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

    /// <summary>
    /// One option per pair, encoded "term|translation", all IsCorrect — grading compares the
    /// user's "term|translation;..." answer as an order-insensitive set against these options.
    /// WordId is null: the question spans several words. Callers guarantee 4-5 words with
    /// group-unique terms/translations, none containing '|' or ';'.
    /// </summary>
    public static AssembledQuestion MatchingPairs(IReadOnlyList<Word> group)
    {
        var options = group.Select(w => new AssembledOption($"{w.Term}|{w.Translation}", true)).ToList();
        return new AssembledQuestion(
            Question.QuestionTypes.MatchingPairs,
            QuestionTemplates.MatchingPairs(group.Select(w => w.Term)),
            string.Join("; ", group.Select(w => $"{w.Term} → {w.Translation}")),
            null,
            options);
    }

    /// <summary>No options; CorrectAnswer is the bare term (the client speaks it via TTS).</summary>
    public static AssembledQuestion ListenAndType(Word word)
    {
        return new AssembledQuestion(
            Question.QuestionTypes.ListenAndType,
            QuestionTemplates.ListenAndType(word.Translation),
            word.Term,
            word.Id,
            []);
    }

    /// <summary>One option per letter of the deterministically scrambled term (SortOrder = position).</summary>
    public static AssembledQuestion WordScramble(Word word)
    {
        var scrambled = Scramble(word.Term, HashCode.Combine(word.Id, Question.QuestionTypes.WordScramble));
        var options = scrambled.Select(c => new AssembledOption(c.ToString(), false)).ToList();
        return new AssembledQuestion(
            Question.QuestionTypes.WordScramble,
            QuestionTemplates.WordScramble(word.Translation),
            word.Term,
            word.Id,
            options);
    }

    /// <summary>
    /// One option per whitespace token of the raw (unblanked) example sentence, in a
    /// deterministically shuffled order. CorrectAnswer keeps the original sentence for grading
    /// (normalized comparison) and feedback display.
    /// </summary>
    public static AssembledQuestion SentenceBuilder(Word word, string rawSentence)
    {
        var tokens = rawSentence.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var shuffled = ShuffleAvoidingIdentity(tokens, HashCode.Combine(word.Id, Question.QuestionTypes.SentenceBuilder));
        var options = shuffled.Select(t => new AssembledOption(t, false)).ToList();
        return new AssembledQuestion(
            Question.QuestionTypes.SentenceBuilder,
            QuestionTemplates.SentenceBuilder(word.Translation),
            rawSentence,
            word.Id,
            options);
    }

    public static AssembledQuestion DefinitionMatch(Word word, string definition, IReadOnlyList<string> distractors)
    {
        var options = BuildOptions(word.Term, distractors, HashCode.Combine(word.Id, Question.QuestionTypes.DefinitionMatch));
        return new AssembledQuestion(
            Question.QuestionTypes.DefinitionMatch,
            QuestionTemplates.DefinitionMatch(definition),
            word.Term,
            word.Id,
            options);
    }

    private static string Scramble(string term, int seed)
    {
        var chars = term.ToCharArray();
        var shuffled = ShuffleAvoidingIdentity(chars, seed);
        return new string(shuffled);
    }

    /// <summary>
    /// Fisher-Yates with a deterministic seed; if the shuffle lands on the original order
    /// (case-insensitively for strings), rotates left by one so the puzzle is never pre-solved.
    /// </summary>
    private static T[] ShuffleAvoidingIdentity<T>(T[] items, int seed)
    {
        var result = (T[])items.Clone();
        var rng = new Random(seed);
        for (var i = result.Length - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (result[i], result[j]) = (result[j], result[i]);
        }

        var isIdentity = result.Length > 1 && result.Zip(items).All(p =>
            string.Equals(p.First?.ToString(), p.Second?.ToString(), StringComparison.OrdinalIgnoreCase));
        if (isIdentity)
        {
            var first = result[0];
            Array.Copy(result, 1, result, 0, result.Length - 1);
            result[^1] = first;
        }

        return result;
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
