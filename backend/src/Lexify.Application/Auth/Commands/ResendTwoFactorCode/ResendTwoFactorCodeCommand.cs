using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Auth.Commands.ResendTwoFactorCode;

/// <summary>Re-issues the sign-in code for an in-flight 2FA challenge (e.g. the first email got lost).</summary>
public sealed record ResendTwoFactorCodeCommand(string ChallengeToken) : IRequest<Result>;
