using FluentValidation;
using Lexify.Domain.Entities;

namespace Lexify.Application.Tests.Commands.GenerateTest;

public sealed class GenerateTestCommandValidator : AbstractValidator<GenerateTestCommand>
{
    public GenerateTestCommandValidator()
    {
        RuleFor(x => x.BlockIds)
            .NotEmpty().WithMessage("At least one block must be selected.")
            .Must(ids => ids.Count <= 10).WithMessage("A maximum of 10 blocks can be selected.")
            .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("Duplicate block IDs are not allowed.");

        RuleFor(x => x.QuestionTypes)
            .NotEmpty().WithMessage("At least one question type must be selected.")
            .Must(types => types.All(Question.QuestionTypes.All.Contains))
            .WithMessage($"Unsupported question type. Supported: {string.Join(", ", Question.QuestionTypes.All)}.");

        RuleFor(x => x.QuestionCount)
            .InclusiveBetween(5, 50)
            .WithMessage("Question count must be between 5 and 50.");
    }
}
