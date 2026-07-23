using FluentValidation;
using Lexify.Application.Feedbacks.Common;
using Lexify.Domain.Entities;

namespace Lexify.Application.Feedbacks.Commands.SubmitFeedback;

public sealed class SubmitFeedbackCommandValidator : AbstractValidator<SubmitFeedbackCommand>
{
    public SubmitFeedbackCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.Type)
            .NotEmpty()
            .Must(Feedback.Types.All.Contains)
            .WithMessage("Unknown feedback type.");

        RuleFor(x => x.Category)
            .Must(c => c is null || Feedback.Categories.All.Contains(c))
            .WithMessage("Unknown feedback category.");

        RuleFor(x => x.Subject).NotEmpty().MinimumLength(5).MaximumLength(150);
        RuleFor(x => x.Message).NotEmpty().MinimumLength(10).MaximumLength(4000);

        RuleFor(x => x.Rating)
            .NotNull().InclusiveBetween((short)1, (short)5)
            .When(x => x.Type == Feedback.Types.Review)
            .WithMessage("A review needs a rating from 1 to 5.");

        RuleFor(x => x.Rating)
            .Null()
            .When(x => x.Type != Feedback.Types.Review)
            .WithMessage("Only a review can carry a rating.");

        RuleFor(x => x.ContactEmail)
            .EmailAddress().MaximumLength(320)
            .When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));

        RuleFor(x => x.Consent)
            .Equal(true)
            .WithMessage("Consent to process the submitted data is required.");

        RuleFor(x => x.Attachments)
            .Must(a => a.Count <= AttachmentRules.MaxCount)
            .WithMessage($"At most {AttachmentRules.MaxCount} attachments are allowed.");

        RuleForEach(x => x.Attachments).ChildRules(a =>
        {
            a.RuleFor(f => f.Content.LongLength)
                .LessThanOrEqualTo(AttachmentRules.MaxSizeBytes)
                .WithMessage($"Each attachment must be at most {AttachmentRules.MaxSizeBytes / (1024 * 1024)} MB.");

            // Decided from the bytes, never from the declared content type or the filename.
            a.RuleFor(f => f.Content)
                .Must(c => AttachmentRules.Sniff(c) is not null)
                .WithMessage("Attachments must be PNG, JPEG, WebP or PDF files.");
        });
    }
}
