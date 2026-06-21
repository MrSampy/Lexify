using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Tests.Commands.DeleteTest;

public sealed record DeleteTestCommand(Guid TestId, Guid UserId) : IRequest<Result>;
