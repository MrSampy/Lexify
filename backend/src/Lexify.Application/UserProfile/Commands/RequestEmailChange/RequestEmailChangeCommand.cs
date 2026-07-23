using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.UserProfile.Commands.RequestEmailChange;

/// <param name="CurrentPassword">Re-authentication: the email address is the account's identity.</param>
public sealed record RequestEmailChangeCommand(
    Guid UserId,
    string NewEmail,
    string CurrentPassword) : IRequest<Result>;
