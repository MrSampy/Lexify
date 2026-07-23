using Lexify.Application.Auth.Commands.Common;
using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Auth.Commands.VerifyTwoFactor;

/// <summary>Step 2 of sign-in: exchange the challenge token + emailed code for a real session.</summary>
public sealed record VerifyTwoFactorCommand(
    string ChallengeToken,
    string Code,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<AuthResponse>>;
