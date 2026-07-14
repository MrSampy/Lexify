using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Auth.Commands.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string? DisplayName,
    /// <summary>Only consulted when public registration is closed; ignored while it is open.</summary>
    string? InviteCode = null) : IRequest<Result<Guid>>;
