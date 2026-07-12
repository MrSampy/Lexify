using FluentValidation;

namespace Lexify.Application.Words.Commands.BulkDeleteWords;

public sealed class BulkDeleteWordsCommandValidator : AbstractValidator<BulkDeleteWordsCommand>
{
    public BulkDeleteWordsCommandValidator()
    {
        RuleFor(x => x.WordIds)
            .NotEmpty().WithMessage("At least one word ID is required.")
            .Must(ids => ids.Count <= 200).WithMessage("Cannot delete more than 200 words at once.");
    }
}
