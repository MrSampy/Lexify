using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Admin.Commands.ChangeUserRole;

public sealed record ChangeUserRoleCommand(Guid UserId, string Role) : IRequest<Result>;
