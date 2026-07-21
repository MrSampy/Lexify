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

    // ---- MatchingPairs ----

    [Fact]
    public void MatchingPairs_EncodesEveryPairAsCorrectOptionWithNullWordId()
    {
        var group = new List<Word>
        {
            MakeWord("dog", "собака"),
            MakeWord("cat", "кіт"),
            MakeWord("bird", "птах"),
            MakeWord("mouse", "миша"),
        };

        var question = TestQuestionAssembler.MatchingPairs(group);

        Assert.Equal(Question.QuestionTypes.MatchingPairs, question.QuestionType);
        Assert.Null(question.WordId);
        Assert.Equal(4, question.Options.Count);
        Assert.All(question.Options, o => Assert.True(o.IsCorrect));
        Assert.Contains(question.Options, o => o.Text == "dog|собака");
        Assert.Contains("dog → собака", question.CorrectAnswer);
        Assert.Contains("dog", question.QuestionText);
    }

    // ---- ListenAndType ----

    [Fact]
    public void ListenAndType_HasNoOptionsAndTermAsCorrectAnswer()
    {
        var word = MakeWord();

        var question = TestQuestionAssembler.ListenAndType(word);

        Assert.Empty(question.Options);
        Assert.Equal(word.Term, question.CorrectAnswer);
        Assert.Contains(word.Translation, question.QuestionText);
        Assert.DoesNotContain(word.Term, question.QuestionText);
    }

    // ---- WordScramble ----

    [Fact]
    public void WordScramble_OptionsArePermutationOfTermAndNeverTheOriginalOrder()
    {
        var word = MakeWord("staggering", "приголомшливий");

        var question = TestQuestionAssembler.WordScramble(word);
        var scrambled = string.Concat(question.Options.Select(o => o.Text));

        Assert.Equal(word.Term, question.CorrectAnswer);
        Assert.Equal(word.Term.Length, question.Options.Count);
        Assert.Equal(word.Term.Order(), scrambled.Order()); // same letters
        Assert.NotEqual(word.Term, scrambled);              // never pre-solved
        Assert.All(question.Options, o => Assert.False(o.IsCorrect));
        Assert.DoesNotContain(word.Term, question.QuestionText);
    }

    [Fact]
    public void WordScramble_IsDeterministicForTheSameWord()
    {
        var word = MakeWord("courage", "сміливість");

        var first = TestQuestionAssembler.WordScramble(word);
        var second = TestQuestionAssembler.WordScramble(word);

        Assert.Equal(first.Options.Select(o => o.Text), second.Options.Select(o => o.Text));
    }

    // ---- SentenceBuilder ----

    [Fact]
    public void SentenceBuilder_OptionsAreExactTokenMultisetInNonOriginalOrder()
    {
        var word = MakeWord();
        const string sentence = "The big dog barked at the mailman loudly.";
        var tokens = sentence.Split(' ');

        var question = TestQuestionAssembler.SentenceBuilder(word, sentence);
        var optionTexts = question.Options.Select(o => o.Text).ToList();

        Assert.Equal(sentence, question.CorrectAnswer);
        Assert.Equal(tokens.Order(), optionTexts.Order());   // exact multiset
        Assert.NotEqual(tokens, optionTexts);                // never pre-solved
        Assert.Contains(word.Translation, question.QuestionText);
    }

    [Fact]
    public void SentenceBuilder_IsDeterministicForTheSameWordAndSentence()
    {
        var word = MakeWord();
        const string sentence = "The dog sleeps on the warm floor.";

        var first = TestQuestionAssembler.SentenceBuilder(word, sentence);
        var second = TestQuestionAssembler.SentenceBuilder(word, sentence);

        Assert.Equal(first.Options.Select(o => o.Text), second.Options.Select(o => o.Text));
    }

    // ---- DefinitionMatch ----

    [Fact]
    public void DefinitionMatch_HasExactlyOneCorrectOptionMatchingTerm()
    {
        var word = MakeWord();
        const string definition = "A loyal four-legged animal often kept as a pet.";

        var question = TestQuestionAssembler.DefinitionMatch(word, definition, ["cat", "mouse", "bird"]);

        Assert.Equal(Question.QuestionTypes.DefinitionMatch, question.QuestionType);
        Assert.Contains(definition, question.QuestionText);
        Assert.Equal(word.Term, question.CorrectAnswer);
        Assert.Single(question.Options.Where(o => o.IsCorrect));
        Assert.Equal(word.Term, question.Options.Single(o => o.IsCorrect).Text);
        Assert.Equal(4, question.Options.Count);
    }
}
