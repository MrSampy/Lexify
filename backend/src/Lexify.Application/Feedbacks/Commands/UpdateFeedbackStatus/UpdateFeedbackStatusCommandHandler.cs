using System.Text.Json;
using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Feedbacks.Commands.UpdateFeedbackStatus;

public sealed class UpdateFeedbackStatusCommandHandler(
    IFeedbackRepository feedbackRepository,
    ICurrentUserService currentUser,
    IAuditService auditService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateFeedbackStatusCommand, Result>
{
    public async Task<Result> Handle(
        UpdateFeedbackStatusCommand request, CancellationToken cancellationToken)
    {
        var feedback = await feedbackRepository.GetByIdAsync(request.FeedbackId, cancellationToken);
        if (feedback is null)
            return Result.NotFound("Feedback not found.");

        var oldStatus = feedback.Status;
        feedback.UpdateStatus(request.Status, request.AdminNote, currentUser.UserId);

        await auditService.LogAsync(
            "update_feedback_status", "Feedback", feedback.Id.ToString(),
            oldValueJson: JsonSerializer.Serialize(oldStatus),
            newValueJson: JsonSerializer.Serialize(feedback.Status),
            ct: cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
