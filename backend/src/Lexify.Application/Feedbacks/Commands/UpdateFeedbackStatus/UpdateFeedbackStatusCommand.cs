using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Feedbacks.Commands.UpdateFeedbackStatus;

public sealed record UpdateFeedbackStatusCommand(
    Guid FeedbackId,
    string Status,
    string? AdminNote)
    : IRequest<Result>;
