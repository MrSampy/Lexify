using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.UserProfile.Commands.DisableTwoFactor;

/// <summary>Turns off a user's 2FA opt-in. Re-authenticated with the current password; blocked for admins.</summary>
public sealed record DisableTwoFactorCommand(Guid UserId, string CurrentPassword) : IRequest<Result>;
