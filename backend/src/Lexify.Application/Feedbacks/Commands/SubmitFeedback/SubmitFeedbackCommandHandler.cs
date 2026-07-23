using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Application.Feedbacks.Common;
using Lexify.Application.Feedbacks.Dtos;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Feedbacks.Commands.SubmitFeedback;

public sealed class SubmitFeedbackCommandHandler(
    IFeedbackRepository feedbackRepository,
    IFeedbackAttachmentStorage attachmentStorage,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SubmitFeedbackCommand, Result<SubmitFeedbackResultDto>>
{
    public async Task<Result<SubmitFeedbackResultDto>> Handle(
        SubmitFeedbackCommand request, CancellationToken cancellationToken)
    {
        var contactEmail = string.IsNullOrWhiteSpace(request.ContactEmail)
            ? currentUser.Email
            : request.ContactEmail.Trim();

        var feedback = Feedback.Create(
            request.UserId, request.Type, request.Category,
            request.Subject, request.Message, request.Rating, contactEmail);

        // Files are written before the row exists, so a failure below would leave orphans on the
        // volume — track what we wrote and clean up if the insert doesn't land.
        var writtenFiles = new List<string>(request.Attachments.Count);
        try
        {
            foreach (var upload in request.Attachments)
            {
                // The validator already rejected unknown types; re-sniff to get the extension the
                // file is actually stored under.
                var sniffed = AttachmentRules.Sniff(upload.Content);
                if (sniffed is null)
                    return Result.Failure<SubmitFeedbackResultDto>(
                        "Attachments must be PNG, JPEG, WebP or PDF files.");

                var storageName = await attachmentStorage.SaveAsync(
                    upload.Content, sniffed.Extension, cancellationToken);
                writtenFiles.Add(storageName);

                feedback.AddAttachment(
                    SafeDisplayName(upload.FileName, sniffed.Extension),
                    sniffed.ContentType,
                    upload.Content.LongLength,
                    storageName);
            }

            await feedbackRepository.AddAsync(feedback, cancellationToken);

            // TicketNumber is an identity column — it only has a value once the INSERT has run.
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            foreach (var storageName in writtenFiles)
                await attachmentStorage.DeleteAsync(storageName, CancellationToken.None);
            throw;
        }

        return Result.Ok(new SubmitFeedbackResultDto(
            feedback.Id, feedback.TicketNumber, TicketCode.From(feedback.TicketNumber)));
    }

    /// <summary>
    /// Keeps the original name recognisable for the admin without storing anything path-like or
    /// long enough to overflow the column. Only ever rendered as text.
    /// </summary>
    private static string SafeDisplayName(string fileName, string extension)
    {
        var name = Path.GetFileName(fileName ?? string.Empty).Trim();
        if (name.Length == 0)
            return $"attachment{extension}";

        return name.Length > 255 ? name[^255..] : name;
    }
}
