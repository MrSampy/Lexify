using Lexify.Application.Common;
using Lexify.Application.Words.Dtos;
using MediatR;

namespace Lexify.Application.Review.Queries.GetDueForReview;

public sealed record GetDueForReviewQuery(Guid UserId, int Limit = 20)
    : IRequest<Result<IReadOnlyList<WordDto>>>;
