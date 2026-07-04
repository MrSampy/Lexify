using FluentValidation;
using Lexify.Domain.Entities;

namespace Lexify.Application.UserProfile.Commands.UpdateEnglishLevel;

public sealed class UpdateEnglishLevelCommandValidator : AbstractValidator<UpdateEnglishLevelCommand>
{
    public UpdateEnglishLevelCommandValidator()
    {
        RuleFor(x => x.EnglishLevel)
            .Must(level => level is null || User.EnglishLevels.All.Contains(level))
            .WithMessage($"English level must be one of: {string.Join(", ", User.EnglishLevels.All)}, or null.");
    }
}
