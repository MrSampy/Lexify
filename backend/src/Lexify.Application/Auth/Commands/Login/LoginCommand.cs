using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Auth.Commands.Login;

public sealed record LoginCommand(
    string Email,
    string Password,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<LoginResponse>>;
