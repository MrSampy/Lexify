using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Auth.Commands.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : IRequest<Result>;
