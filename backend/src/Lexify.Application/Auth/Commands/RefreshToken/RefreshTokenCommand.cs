using Lexify.Application.Auth.Commands.Common;
using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand(
    string Token,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<AuthResponse>>;
