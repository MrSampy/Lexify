using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Admin.Commands.ImpersonateUser;

public sealed record ImpersonateUserCommand(Guid TargetUserId) : IRequest<Result<string>>;
