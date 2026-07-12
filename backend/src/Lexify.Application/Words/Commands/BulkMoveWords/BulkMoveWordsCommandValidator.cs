using FluentValidation;

namespace Lexify.Application.Words.Commands.BulkMoveWords;

public sealed class BulkMoveWordsCommandValidator : AbstractValidator<BulkMoveWordsCommand>
{
    public BulkMoveWordsCommandValidator()
    {
        RuleFor(x => x.TargetBlockId)
            .NotEmpty().WithMessage("Target block ID is required.");

        RuleFor(x => x.WordIds)
            .NotEmpty().WithMessage("At least one word ID is required.")
            .Must(ids => ids.Count <= 200).WithMessage("Cannot move more than 200 words at once.");
    }
}
