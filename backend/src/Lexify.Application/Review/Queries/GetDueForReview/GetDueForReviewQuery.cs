using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Review.Queries.GetDueForReview;

public sealed record GetDueForReviewQuery(
    Guid UserId, int Limit = 20, Guid? BlockId = null, bool Cram = false)
    : IRequest<Result<ReviewQueueDto>>;
