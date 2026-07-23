namespace Lexify.Application.Auth.Common;

public static class AuthErrorCodes
{
    /// <summary>
    /// Sign-in was refused only because the address is unconfirmed. Returned as the failure message so
    /// the API layer can translate it into a machine-readable <c>code</c>: the client has to tell this
    /// apart from bad credentials to offer "resend the confirmation email" instead of "check your password".
    /// </summary>
    public const string EmailNotVerified = "email_not_verified";

    /// <summary>
    /// The password was correct but a second factor is still owed. Unlike <see cref="EmailNotVerified"/>
    /// this rides on a *successful* login result (the handler returns a challenge, not a failure); the
    /// constant exists so the client and tests share one spelling of the marker.
    /// </summary>
    public const string TwoFactorRequired = "two_factor_required";
}
