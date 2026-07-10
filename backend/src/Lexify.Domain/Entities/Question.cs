using System.Security.Cryptography;
using System.Text;
using Lexify.Domain.Common;

namespace Lexify.Domain.Entities;

public sealed class Question
{
    private readonly List<QuestionOption> _options = [];

    public Guid Id { get; private set; }
    public Guid TestId { get; private set; }
    public Guid? WordId { get; private set; }
    public string QuestionType { get; private set; } = default!;
    public string QuestionText { get; private set; } = default!;
    public string CorrectAnswer { get; private set; } = default!;
    public int SortOrder { get; private set; }
    public string ContentHash { get; private set; } = default!;

    public IReadOnlyCollection<QuestionOption> Options => _options.AsReadOnly();

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

    public static Question Create(
        Guid testId,
        Guid? wordId,
        string questionType,
        string questionText,
        string correctAnswer,
        int sortOrder)
    {
        if (testId == Guid.Empty) throw new DomainException("Test ID cannot be empty.");
        if (!QuestionTypes.All.Contains(questionType)) throw new DomainException($"Invalid question type: '{questionType}'.");
        if (string.IsNullOrWhiteSpace(questionText)) throw new DomainException("Question text cannot be empty.");
        if (string.IsNullOrWhiteSpace(correctAnswer)) throw new DomainException("Correct answer cannot be empty.");

        var contentHash = ComputeContentHash(questionType, questionText);
        return new Question(testId, wordId, questionType, questionText, correctAnswer, sortOrder, contentHash);
    }

    /// <summary>
    /// SHA-256(questionType|questionText). Exposed publicly so callers can predict the hash of a
    /// question before it's actually assembled — used by GenerateTestJob's planning step to prefer
    /// (word, type) combinations this user hasn't already seen in a prior test.
    /// </summary>
    public static string ComputeContentHash(string questionType, string questionText)
    {
        var input = $"{questionType}|{questionText}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static class QuestionTypes
    {
        public const string TranslateToNative = "translate_to_native";
        public const string TranslateToForeign = "translate_to_foreign";
        public const string FillInSentence = "fill_in_sentence";
        public const string MultiSelectTheme = "multi_select_theme";
        public const string OpenAnswer = "open_answer";

        public static readonly IReadOnlySet<string> All =
            new HashSet<string> { TranslateToNative, TranslateToForeign, FillInSentence, MultiSelectTheme, OpenAnswer };
    }
}
