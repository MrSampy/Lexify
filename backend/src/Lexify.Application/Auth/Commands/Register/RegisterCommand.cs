using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Auth.Commands.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string? DisplayName) : IRequest<Result<Guid>>;
