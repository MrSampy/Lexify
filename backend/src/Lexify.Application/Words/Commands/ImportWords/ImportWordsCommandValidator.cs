using FluentValidation;
using Lexify.Domain.Entities;

namespace Lexify.Application.Words.Commands.ImportWords;

public sealed class ImportWordsCommandValidator : AbstractValidator<ImportWordsCommand>
{
    public ImportWordsCommandValidator()
    {
        RuleFor(x => x.BlockId)
            .NotEmpty().WithMessage("Block ID is required.");

        RuleFor(x => x.Words)
            .NotEmpty().WithMessage("Words list cannot be empty.")
            .Must(w => w.Count <= 200).WithMessage("Cannot import more than 200 words at once.");

        RuleForEach(x => x.Words).ChildRules(word =>
        {
            word.RuleFor(w => w.Term)
                .NotEmpty().WithMessage("Term is required.")
                .MaximumLength(500).WithMessage("Term must not exceed 500 characters.");

            word.RuleFor(w => w.Translation)
                .NotEmpty().WithMessage("Translation is required.")
                .MaximumLength(500).WithMessage("Translation must not exceed 500 characters.");

            word.RuleFor(w => w.WordType)
                .Must(t => Word.WordTypes.All.Contains(t))
                .WithMessage($"Word type must be one of: {string.Join(", ", Word.WordTypes.All)}.");
        });
    }
}
