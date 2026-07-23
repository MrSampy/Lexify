using FluentValidation;
using Lexify.Domain.Entities;

namespace Lexify.Application.Feedbacks.Commands.UpdateFeedbackStatus;

public sealed class UpdateFeedbackStatusCommandValidator : AbstractValidator<UpdateFeedbackStatusCommand>
{
    public UpdateFeedbackStatusCommandValidator()
    {
        RuleFor(x => x.FeedbackId).NotEmpty();

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(Feedback.Statuses.All.Contains)
            .WithMessage("Unknown feedback status.");

        RuleFor(x => x.AdminNote).MaximumLength(2000);
    }
}
