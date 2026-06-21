using Lexify.Application.Common;
using Lexify.Application.Tests.Dtos;
using MediatR;

namespace Lexify.Application.Tests.Commands.SubmitAnswer;

public sealed record SubmitAnswerCommand(
    Guid AttemptId,
    Guid QuestionId,
    Guid UserId,
    string GivenAnswer,
    int? TimeSpentMs) : IRequest<Result<AnswerFeedbackDto>>;
