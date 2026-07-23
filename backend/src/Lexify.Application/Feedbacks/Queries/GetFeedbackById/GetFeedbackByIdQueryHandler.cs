using Lexify.Application.Common;
using Lexify.Application.Feedbacks.Common;
using Lexify.Application.Feedbacks.Dtos;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Feedbacks.Queries.GetFeedbackById;

public sealed class GetFeedbackByIdQueryHandler(IFeedbackRepository feedbackRepository)
    : IRequestHandler<GetFeedbackByIdQuery, Result<FeedbackDetailDto>>
{
    public async Task<Result<FeedbackDetailDto>> Handle(
        GetFeedbackByIdQuery request, CancellationToken cancellationToken)
    {
        var feedback = await feedbackRepository.GetByIdWithAttachmentsAsync(
            request.FeedbackId, cancellationToken);

        if (feedback is null)
            return Result.NotFound<FeedbackDetailDto>("Feedback not found.");

        var email = await feedbackRepository.GetAuthorEmailAsync(feedback.Id, cancellationToken);

        return Result.Ok(FeedbackMapping.ToDetail(feedback, email));
    }
}
