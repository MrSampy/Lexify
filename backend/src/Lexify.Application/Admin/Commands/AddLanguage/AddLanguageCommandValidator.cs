using FluentValidation;

namespace Lexify.Application.Admin.Commands.AddLanguage;

public sealed class AddLanguageCommandValidator : AbstractValidator<AddLanguageCommand>
{
    public AddLanguageCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Language code is required.")
            .MaximumLength(10).WithMessage("Language code must not exceed 10 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Language name is required.")
            .MaximumLength(100).WithMessage("Language name must not exceed 100 characters.");

        RuleFor(x => x.NativeName)
            .NotEmpty().WithMessage("Native name is required.")
            .MaximumLength(100).WithMessage("Native name must not exceed 100 characters.");
    }
}
