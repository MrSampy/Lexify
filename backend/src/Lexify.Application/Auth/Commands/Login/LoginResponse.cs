using Lexify.Application.Auth.Commands.Common;

namespace Lexify.Application.Auth.Commands.Login;

/// <summary>
/// The outcome of step 1 of sign-in. Either the session was issued outright (no 2FA owed), or a
/// second factor is required — in which case a short-lived challenge token is returned instead of any
/// real credentials, and the client must complete <c>login/verify-2fa</c> to obtain a session.
/// </summary>
public sealed record LoginResponse(AuthResponse? Session, string? TwoFactorChallenge)
{
    public bool TwoFactorRequired => TwoFactorChallenge is not null;

    public static LoginResponse Authenticated(AuthResponse session) => new(session, null);

    public static LoginResponse Challenge(string challengeToken) => new(null, challengeToken);
}
