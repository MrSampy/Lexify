using FluentValidation;

namespace Lexify.Application.Review.Commands.ReviewWord;

public sealed class ReviewWordCommandValidator : AbstractValidator<ReviewWordCommand>
{
    public ReviewWordCommandValidator()
    {
        RuleFor(x => x.WordId)
            .NotEmpty().WithMessage("Word ID is required.");

        RuleFor(x => x.Quality)
            .InclusiveBetween(0, 5).WithMessage("Quality must be between 0 and 5.");
    }
}
