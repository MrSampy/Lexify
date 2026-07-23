using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.UserProfile.Commands.EnableTwoFactor;

/// <summary>
/// Step 1 of a user opting into 2FA: emails a confirmation code. The flag is only flipped once the code
/// is confirmed (<see cref="ConfirmEnableTwoFactor.ConfirmEnableTwoFactorCommand"/>), proving the user
/// can actually receive the codes before they get locked behind them.
/// </summary>
public sealed record EnableTwoFactorCommand(Guid UserId) : IRequest<Result>;
