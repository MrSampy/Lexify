using Lexify.Application.Auth.Commands.Common;
using Lexify.Application.Auth.Commands.ResendVerification;
using Lexify.Application.Auth.Commands.VerifyEmail;
using Lexify.Application.Auth.Common;
using Lexify.Application.Auth.Commands.ForgotPassword;
using Lexify.Application.Auth.Commands.Login;
using Lexify.Application.Auth.Commands.Logout;
using Lexify.Application.Auth.Commands.RefreshToken;
using Lexify.Application.Auth.Commands.Register;
using Lexify.Application.Auth.Commands.ResendTwoFactorCode;
using Lexify.Application.Auth.Commands.ResetPassword;
using Lexify.Application.Auth.Commands.VerifyTwoFactor;
using Lexify.Application.Auth.Queries.GetRegistrationStatus;
using Lexify.API.RateLimit;
using Lexify.API.Requests.Auth;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Lexify.API.Controllers;

[Route("api/auth")]
[EnableRateLimiting(AuthRateLimiterPolicy.PolicyName)]
public sealed class AuthController(ISender sender) : BaseApiController
{
    // The raw refresh token never reaches JavaScript: it lives in an HttpOnly cookie scoped to the
    // auth endpoints only. The response body carries just the access token + its expiry.
    private const string RefreshTokenCookie = "lexify_rt";

    // Must match RefreshTokenLifetime in Login/RefreshToken command handlers.
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    /// <summary>Register a new user account.</summary>
    [HttpPost("register")]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RegisterCommand(request.Email, request.Password, request.DisplayName, request.InviteCode),
            cancellationToken);

        return ToActionResult(result, id => CreatedAtAction(nameof(Register), new { id }, id));
    }

    /// <summary>Whether sign-up is open, and whether it needs an invite code. Never returns the code itself.</summary>
    [HttpGet("registration-status")]
    [ProducesResponseType<RegistrationStatusDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRegistrationStatus(CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(new GetRegistrationStatusQuery(), cancellationToken));

    /// <summary>Log in; access token is returned in the body, refresh token is set as an HttpOnly cookie.</summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var command = new LoginCommand(
            request.Email,
            request.Password,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        var result = await sender.Send(command, cancellationToken);

        // The client has to tell "confirm your email" apart from "wrong password" to offer the resend
        // button, so this one failure carries a machine-readable code instead of a bare message.
        if (result.ErrorMessage == AuthErrorCodes.EmailNotVerified)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                code = AuthErrorCodes.EmailNotVerified,
                message = "Please confirm your email address before signing in."
            });
        }

        // A second factor is owed: hand back the challenge (no session, no refresh cookie) so the client
        // can collect the emailed code and complete login/verify-2fa. Distinguished from a real login by
        // the flag rather than an error, since the password step itself succeeded.
        return ToActionResult(result, login => login.TwoFactorRequired
            ? Ok(new { twoFactorRequired = true, challengeToken = login.TwoFactorChallenge })
            : ToAuthResult(login.Session!));
    }

    /// <summary>
    /// Step 2 of sign-in: exchange the challenge token + the emailed code for a session. Rate-limited
    /// tighter than the rest of auth to blunt brute-forcing of the 6-digit code.
    /// </summary>
    [HttpPost("login/verify-2fa")]
    [EnableRateLimiting(TwoFactorRateLimiterPolicy.PolicyName)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> VerifyTwoFactor(
        VerifyTwoFactorRequest request, CancellationToken cancellationToken)
    {
        var command = new VerifyTwoFactorCommand(
            request.ChallengeToken,
            request.Code,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        var result = await sender.Send(command, cancellationToken);

        // A dead challenge means "start over", a wrong code means "try again here" — the client can only
        // tell them apart from a machine-readable code, so this one failure carries it (wrong/used/locked
        // codes stay a bare generic failure to avoid leaking code state).
        if (result.ErrorMessage == AuthErrorCodes.TwoFactorChallengeExpired)
        {
            return BadRequest(new
            {
                code = AuthErrorCodes.TwoFactorChallengeExpired,
                message = "Your sign-in session expired. Please sign in again."
            });
        }

        return ToActionResult(result, ToAuthResult);
    }

    /// <summary>Re-sends the sign-in code for an in-flight 2FA challenge.</summary>
    [HttpPost("login/resend-2fa")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendTwoFactor(
        ResendTwoFactorRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(
            new ResendTwoFactorCodeCommand(request.ChallengeToken), cancellationToken));

    /// <summary>Confirm an email address using the token from the link. Single-use.</summary>
    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> VerifyEmail(
        VerifyEmailRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(new VerifyEmailCommand(request.Token), cancellationToken));

    /// <summary>
    /// Send the confirmation email again. Always returns 200 so the response reveals nothing about
    /// account existence or confirmation state.
    /// </summary>
    [HttpPost("resend-verification")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ResendVerification(
        ResendVerificationRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new ResendVerificationCommand(request.Email), cancellationToken);
        return Ok();
    }

    /// <summary>Exchange the refresh-token cookie for a new token pair (cookie is rotated).</summary>
    /// <remarks>
    /// Deliberately exempt from the auth rate limiter. That policy partitions by IP, and a whole office
    /// or mobile carrier behind one NAT address shares its 30-per-15-minutes budget — once exhausted,
    /// every silent refresh 429s and everyone gets bounced to the sign-in screen. Possession of a valid
    /// refresh cookie is itself the gate here; nothing is guessable by an unauthenticated caller.
    /// </remarks>
    [HttpPost("refresh")]
    [DisableRateLimiting]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(RefreshTokenCookie, out var token) || string.IsNullOrEmpty(token))
            return BadRequest(new { message = "No refresh token." });

        var command = new RefreshTokenCommand(
            token,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        var result = await sender.Send(command, cancellationToken);

        // Only clear the cookie when the token behind it is provably finished. Clearing on *any*
        // failure is what used to end healthy sessions: one request losing the rotation race, or a
        // single 429, wiped the cookie for every tab at once.
        if (result.ErrorMessage == AuthErrorCodes.RefreshTokenDead)
            DeleteRefreshCookie();

        return ToActionResult(result, ToAuthResult);
    }

    /// <summary>Revoke the refresh token and clear its cookie (logout).</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (Request.Cookies.TryGetValue(RefreshTokenCookie, out var token) && !string.IsNullOrEmpty(token))
            await sender.Send(new LogoutCommand(token), cancellationToken);

        DeleteRefreshCookie();
        return Ok();
    }

    /// <summary>Request a password-reset email. Always returns 200 so the response reveals nothing about account existence.</summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new ForgotPasswordCommand(request.Email), cancellationToken);
        return Ok();
    }

    /// <summary>Set a new password using a token from the reset email. The token is single-use.</summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ResetPasswordCommand(request.Token, request.NewPassword),
            cancellationToken);

        return ToActionResult(result);
    }

    private IActionResult ToAuthResult(AuthResponse auth)
    {
        // No raw token means the session was served inside the rotation grace window: the cookie the
        // browser holds is already the live successor, so writing one here would only risk clobbering
        // it with a stale value if the responses come back out of order.
        if (!string.IsNullOrEmpty(auth.RefreshToken))
            Response.Cookies.Append(RefreshTokenCookie, auth.RefreshToken, RefreshCookieOptions());

        return Ok(new { auth.AccessToken, auth.ExpiresAt });
    }

    private void DeleteRefreshCookie() =>
        Response.Cookies.Delete(RefreshTokenCookie, RefreshCookieOptions());

    private static CookieOptions RefreshCookieOptions() => new()
    {
        HttpOnly = true,
        // Frontend (5173) and API run on different origins, so the cookie must be SameSite=None,
        // which requires Secure. Browsers treat http://localhost as trustworthy, so this works in dev.
        Secure = true,
        SameSite = SameSiteMode.None,
        // Only auth endpoints ever need the refresh token — don't send it with every API request.
        Path = "/api/auth",
        MaxAge = RefreshTokenLifetime
    };
}
