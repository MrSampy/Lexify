using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.UserProfile.Commands.ConfirmEnableTwoFactor;

/// <summary>Step 2 of opting in: confirm the emailed code, which flips the opt-in flag on.</summary>
public sealed record ConfirmEnableTwoFactorCommand(Guid UserId, string Code) : IRequest<Result>;
