using FluentValidation;
using Lexify.Domain.Entities;

namespace Lexify.Application.UserProfile.Commands.UpdateReviewSettings;

public sealed class UpdateReviewSettingsCommandValidator : AbstractValidator<UpdateReviewSettingsCommand>
{
    public UpdateReviewSettingsCommandValidator()
    {
        RuleFor(x => x.NewWordsPerDay)
            .InclusiveBetween(0, User.MaxNewWordsPerDay)
            .WithMessage($"New words per day must be between 0 and {User.MaxNewWordsPerDay}.");
    }
}
