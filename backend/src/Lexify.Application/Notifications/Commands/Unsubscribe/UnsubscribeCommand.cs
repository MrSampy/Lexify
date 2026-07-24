using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Notifications.Commands.Unsubscribe;

/// <param name="Token">The signed token from the unsubscribe link in a reminder email.</param>
public sealed record UnsubscribeCommand(string Token) : IRequest<Result>;
