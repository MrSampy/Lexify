using Lexify.Application.Common;
using Lexify.Application.Tests.Dtos;
using MediatR;

namespace Lexify.Application.Tests.Commands.StartAttempt;

public sealed record StartAttemptCommand(Guid TestId, Guid UserId) : IRequest<Result<StartAttemptResult>>;
