using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.UserProfile.Commands.UpdateDisplayName;

public sealed record UpdateDisplayNameCommand(Guid UserId, string? DisplayName) : IRequest<Result>;
