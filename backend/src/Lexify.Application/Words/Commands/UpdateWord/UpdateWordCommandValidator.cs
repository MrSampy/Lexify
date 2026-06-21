using FluentValidation;

namespace Lexify.Application.Words.Commands.UpdateWord;

public sealed class UpdateWordCommandValidator : AbstractValidator<UpdateWordCommand>
{
    public UpdateWordCommandValidator()
    {
        RuleFor(x => x.WordId)
            .NotEmpty().WithMessage("Word ID is required.");

        RuleFor(x => x.Translation)
            .NotEmpty().WithMessage("Translation is required.")
            .MaximumLength(500).WithMessage("Translation must not exceed 500 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.")
            .When(x => x.Notes is not null);

        RuleFor(x => x.ExampleSentence)
            .MaximumLength(1000).WithMessage("Example sentence must not exceed 1000 characters.")
            .When(x => x.ExampleSentence is not null);

        RuleFor(x => x.ConfidenceNote)
            .MaximumLength(500).WithMessage("Confidence note must not exceed 500 characters.")
            .When(x => x.ConfidenceNote is not null);
    }
}
