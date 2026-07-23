using FluentValidation;

namespace Lexify.Application.UserProfile.Commands.ConfirmEnableTwoFactor;

public sealed class ConfirmEnableTwoFactorCommandValidator : AbstractValidator<ConfirmEnableTwoFactorCommand>
{
    public ConfirmEnableTwoFactorCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.")
            .Matches("^[0-9]{6}$").WithMessage("Code must be 6 digits.");
    }
}
