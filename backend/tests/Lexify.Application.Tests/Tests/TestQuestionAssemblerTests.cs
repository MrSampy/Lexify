using Lexify.Application.Tests.Services;
using Lexify.Domain.Entities;

namespace Lexify.Application.Tests.Tests;

public class TestQuestionAssemblerTests
{
    private static Word MakeWord(string term = "dog", string translation = "собака") =>
        new(Guid.NewGuid(), term, translation);

    [Fact]
    public void TranslateToNative_HasExactlyOneCorrectOptionMatchingTranslation()
    {
        var word = MakeWord();
        var question = TestQuestionAssembler.TranslateToNative(word, ["кіт", "миша", "птах"]);

        Assert.Equal(Question.QuestionTypes.TranslateToNative, question.QuestionType);
        Assert.Contains(word.Term, question.QuestionText);
        Assert.Single(question.Options.Where(o => o.IsCorrect));
        Assert.Equal(word.Translation, question.Options.Single(o => o.IsCorrect).Text);
        Assert.Equal(4, question.Options.Count);
    }

    [Fact]
    public void TranslateToNative_DistractorsExcludeDuplicatesOfCorrectAnswer()
    {
        var word = MakeWord();
        var question = TestQuestionAssembler.TranslateToNative(word, [word.Translation, "кіт", "миша"]);

        Assert.Single(question.Options.Where(o => o.Text == word.Translation));
    }

    [Fact]
    public void TranslateToForeign_QuestionTextIncludesLanguageNameAndTranslation()
    {
        var word = MakeWord();
        var question = TestQuestionAssembler.TranslateToForeign(word, "English", ["cat", "mouse", "bird"]);

        Assert.Equal(Question.QuestionTypes.TranslateToForeign, question.QuestionType);
        Assert.Contains("English", question.QuestionText);
        Assert.Contains(word.Translation, question.QuestionText);
        Assert.Equal(word.Term, question.CorrectAnswer);
    }

    [Fact]
    public void MultiSelectTheme_CorrectAnswerListsTranslationAndAlternatives()
    {
        var word = MakeWord();
        word.SetAlternativeTranslations(["пес"]);

        var question = TestQuestionAssembler.MultiSelectTheme(word, ["кіт", "миша", "птах"]);
        var correctOptions = question.Options.Where(o => o.IsCorrect).Select(o => o.Text).ToList();

        Assert.Contains(word.Translation, correctOptions);
        Assert.Contains("пес", correctOptions);
        Assert.Contains(word.Translation, question.CorrectAnswer);
        Assert.Contains("пес", question.CorrectAnswer);
    }

    [Fact]
    public void MultiSelectTheme_LimitsAlternativesToTwo()
    {
        var word = MakeWord();
        word.SetAlternativeTranslations(["пес", "песик", "собачка"]);

        var question = TestQuestionAssembler.MultiSelectTheme(word, ["кіт", "миша"]);

        Assert.Equal(3, question.Options.Count(o => o.IsCorrect)); // primary + 2 alternatives, not 3
    }

    [Fact]
    public void OpenAnswer_HasNoOptionsAndCorrectAnswerListsAllTranslations()
    {
        var word = MakeWord();
        word.SetAlternativeTranslations(["пес"]);

        var question = TestQuestionAssembler.OpenAnswer(word);

        Assert.Empty(question.Options);
        Assert.Contains(word.Translation, question.CorrectAnswer);
        Assert.Contains("пес", question.CorrectAnswer);
    }

    [Fact]
    public void FillInSentence_UsesBlankedSentenceAsQuestionTextAndTermAsCorrectAnswer()
    {
        var word = MakeWord();
        const string blanked = "The ___ barked loudly.";

        var question = TestQuestionAssembler.FillInSentence(word, blanked, ["cat", "mouse", "bird"]);

        Assert.Equal(blanked, question.QuestionText);
        Assert.Equal(word.Term, question.CorrectAnswer);
        Assert.Single(question.Options.Where(o => o.IsCorrect));
    }

    [Fact]
    public void BuildOptions_ShuffleIsDeterministicForTheSameWordAndType()
    {
        var word = MakeWord();
        var distractors = new[] { "кіт", "миша", "птах" };

        var first = TestQuestionAssembler.TranslateToNative(word, distractors);
        var second = TestQuestionAssembler.TranslateToNative(word, distractors);

        Assert.Equal(first.Options.Select(o => o.Text), second.Options.Select(o => o.Text));
    }
}
