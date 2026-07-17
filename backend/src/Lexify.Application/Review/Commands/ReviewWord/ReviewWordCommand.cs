using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Review.Commands.ReviewWord;

public sealed record ReviewWordCommand(Guid WordId, Guid UserId, int Quality)
    : IRequest<Result<RateWordResultDto>>;
