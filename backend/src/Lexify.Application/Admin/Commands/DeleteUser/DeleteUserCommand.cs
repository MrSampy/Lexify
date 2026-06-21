using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Admin.Commands.DeleteUser;

public sealed record DeleteUserCommand(Guid UserId) : IRequest<Result>;
