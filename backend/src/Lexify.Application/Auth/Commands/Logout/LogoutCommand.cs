using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Auth.Commands.Logout;

public sealed record LogoutCommand(string Token) : IRequest<Result>;
