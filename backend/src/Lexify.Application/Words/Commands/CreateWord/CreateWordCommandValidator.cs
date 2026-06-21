using FluentValidation;
using Lexify.Domain.Entities;

namespace Lexify.Application.Words.Commands.CreateWord;

public sealed class CreateWordCommandValidator : AbstractValidator<CreateWordCommand>
{
    public CreateWordCommandValidator()
    {
        RuleFor(x => x.BlockId)
            .NotEmpty().WithMessage("Block ID is required.");

        RuleFor(x => x.Term)
            .NotEmpty().WithMessage("Term is required.")
            .MaximumLength(500).WithMessage("Term must not exceed 500 characters.");

        RuleFor(x => x.Translation)
            .NotEmpty().WithMessage("Translation is required.")
            .MaximumLength(500).WithMessage("Translation must not exceed 500 characters.");

        RuleFor(x => x.WordType)
            .Must(t => Word.WordTypes.All.Contains(t))
            .WithMessage($"Word type must be one of: {string.Join(", ", Word.WordTypes.All)}.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.")
            .When(x => x.Notes is not null);

        RuleFor(x => x.ExampleSentence)
            .MaximumLength(1000).WithMessage("Example sentence must not exceed 1000 characters.")
            .When(x => x.ExampleSentence is not null);
    }
}
