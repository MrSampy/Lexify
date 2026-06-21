using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Tests.Commands.FinishAttempt;

public sealed record FinishAttemptCommand(Guid AttemptId, Guid UserId) : IRequest<Result>;
