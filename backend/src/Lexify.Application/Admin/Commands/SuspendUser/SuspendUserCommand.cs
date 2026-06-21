using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Admin.Commands.SuspendUser;

public sealed record SuspendUserCommand(Guid UserId) : IRequest<Result>;
