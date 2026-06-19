namespace Lexify.Domain.Entities;

public sealed class QuestionOption
{
    public Guid Id { get; private set; }
    public Guid QuestionId { get; private set; }
    public string OptionText { get; private set; } = default!;
    public bool IsCorrect { get; private set; }
    public int SortOrder { get; private set; }

    private QuestionOption() { }

    public QuestionOption(Guid questionId, string optionText, bool isCorrect, int sortOrder)
    {
        Id = Guid.NewGuid();
        QuestionId = questionId;
        OptionText = optionText;
        IsCorrect = isCorrect;
        SortOrder = sortOrder;
    }
}
