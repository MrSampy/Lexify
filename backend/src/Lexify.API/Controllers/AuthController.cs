using Lexify.Application.Auth.Commands.Common;
using Lexify.Application.Auth.Commands.ForgotPassword;
using Lexify.Application.Auth.Commands.Login;
using Lexify.Application.Auth.Commands.Logout;
using Lexify.Application.Auth.Commands.RefreshToken;
using Lexify.Application.Auth.Commands.Register;
using Lexify.Application.Auth.Commands.ResetPassword;
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

        return ToActionResult(await sender.Send(command, cancellationToken), ToAuthResult);
    }

    /// <summary>Exchange the refresh-token cookie for a new token pair (cookie is rotated).</summary>
    [HttpPost("refresh")]
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
        if (!result.IsSuccess)
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
