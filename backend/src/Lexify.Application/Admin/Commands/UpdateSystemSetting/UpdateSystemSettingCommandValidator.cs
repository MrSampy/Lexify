using FluentValidation;

namespace Lexify.Application.Admin.Commands.UpdateSystemSetting;

public sealed class UpdateSystemSettingCommandValidator : AbstractValidator<UpdateSystemSettingCommand>
{
    public UpdateSystemSettingCommandValidator()
    {
        RuleFor(x => x.Key).NotEmpty().WithMessage("Setting key is required.");
        RuleFor(x => x.Value).NotNull().WithMessage("Setting value is required.");
    }
}
