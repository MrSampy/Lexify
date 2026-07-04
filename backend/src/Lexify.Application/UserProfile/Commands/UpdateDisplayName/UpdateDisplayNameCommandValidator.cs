using FluentValidation;

namespace Lexify.Application.UserProfile.Commands.UpdateDisplayName;

public sealed class UpdateDisplayNameCommandValidator : AbstractValidator<UpdateDisplayNameCommand>
{
    public UpdateDisplayNameCommandValidator()
    {
        RuleFor(x => x.DisplayName)
            .MaximumLength(64).WithMessage("Display name must not exceed 64 characters.")
            .When(x => x.DisplayName is not null);
    }
}
