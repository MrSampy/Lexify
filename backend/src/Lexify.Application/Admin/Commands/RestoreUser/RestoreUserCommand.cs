using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Admin.Commands.RestoreUser;

public sealed record RestoreUserCommand(Guid UserId) : IRequest<Result>;
