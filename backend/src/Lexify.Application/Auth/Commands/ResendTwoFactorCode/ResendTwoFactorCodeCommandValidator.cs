using FluentValidation;

namespace Lexify.Application.Auth.Commands.ResendTwoFactorCode;

public sealed class ResendTwoFactorCodeCommandValidator : AbstractValidator<ResendTwoFactorCodeCommand>
{
    public ResendTwoFactorCodeCommandValidator()
    {
        RuleFor(x => x.ChallengeToken)
            .NotEmpty().WithMessage("Challenge token is required.");
    }
}
