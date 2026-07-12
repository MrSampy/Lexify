using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Auth.Commands.ResetPassword;

public sealed record ResetPasswordCommand(string Token, string NewPassword) : IRequest<Result>;
