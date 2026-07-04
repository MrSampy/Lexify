using FluentValidation;

namespace Lexify.Application.Tests.Commands.SubmitAnswer;

public sealed class SubmitAnswerCommandValidator : AbstractValidator<SubmitAnswerCommand>
{
    public SubmitAnswerCommandValidator()
    {
        RuleFor(x => x.AttemptId).NotEmpty();
        RuleFor(x => x.QuestionId).NotEmpty();

        RuleFor(x => x.GivenAnswer)
            .NotNull().WithMessage("Answer is required.")
            .MaximumLength(2000).WithMessage("Answer must not exceed 2000 characters.");

        RuleFor(x => x.TimeSpentMs)
            .GreaterThanOrEqualTo(0).When(x => x.TimeSpentMs.HasValue)
            .WithMessage("Time spent cannot be negative.");
    }
}
