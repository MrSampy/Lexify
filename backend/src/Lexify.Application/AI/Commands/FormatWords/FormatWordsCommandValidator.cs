using FluentValidation;

namespace Lexify.Application.AI.Commands.FormatWords;

public sealed class FormatWordsCommandValidator : AbstractValidator<FormatWordsCommand>
{
    private static readonly HashSet<string> ValidLanguageCodes =
        new(["en", "no", "uk", "ru", "de", "pl", "fr", "es", "it"], StringComparer.OrdinalIgnoreCase);

    public FormatWordsCommandValidator()
    {
        RuleFor(x => x.RawText)
            .NotEmpty().WithMessage("Raw text is required.")
            .MaximumLength(10_000).WithMessage("Raw text must not exceed 10,000 characters.");

        RuleFor(x => x.TargetLanguage)
            .NotEmpty().WithMessage("Target language is required.")
            .Must(code => ValidLanguageCodes.Contains(code))
            .WithMessage("Target language is not a supported language code.");

        RuleFor(x => x.NativeLanguage)
            .NotEmpty().WithMessage("Native language is required.");
    }
}
