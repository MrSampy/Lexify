using FluentValidation;

namespace Lexify.Application.Blocks.Commands.AddTagToBlock;

public sealed class AddTagToBlockCommandValidator : AbstractValidator<AddTagToBlockCommand>
{
    public AddTagToBlockCommandValidator()
    {
        RuleFor(x => x.BlockId).NotEmpty();

        RuleFor(x => x.TagName)
            .NotEmpty().WithMessage("Tag name is required.")
            .MaximumLength(50).WithMessage("Tag name must not exceed 50 characters.");
    }
}
