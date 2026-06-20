namespace Lexify.Domain.Entities;

public sealed class Question
{
    public Guid Id { get; private set; }
    public Guid TestId { get; private set; }
    public Guid? WordId { get; private set; }
    public string QuestionType { get; private set; } = default!;
    public string QuestionText { get; private set; } = default!;
    public string CorrectAnswer { get; private set; } = default!;
    public int SortOrder { get; private set; }
    public string ContentHash { get; private set; } = default!;

    private Question() { }

    public Question(Guid testId, Guid? wordId, string questionType, string questionText,
        string correctAnswer, int sortOrder, string contentHash)
    {
        Id = Guid.NewGuid();
        TestId = testId;
        WordId = wordId;
        QuestionType = questionType;
        QuestionText = questionText;
        CorrectAnswer = correctAnswer;
        SortOrder = sortOrder;
        ContentHash = contentHash;
    }

    public static class QuestionTypes
    {
        public const string TranslateToNative = "translate_to_native";
        public const string TranslateToForeign = "translate_to_foreign";
        public const string FillInSentence = "fill_in_sentence";
        public const string MultiSelectTheme = "multi_select_theme";
        public const string OpenAnswer = "open_answer";
    }
}
