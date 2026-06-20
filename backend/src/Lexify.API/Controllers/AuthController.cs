using Lexify.Application.Auth.Commands.Login;
using Lexify.Application.Auth.Commands.Logout;
using Lexify.Application.Auth.Commands.RefreshToken;
using Lexify.Application.Auth.Commands.Register;
using Lexify.API.Requests.Auth;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lexify.API.Controllers;

[Route("api/auth")]
public sealed class AuthController(ISender sender) : BaseApiController
{
    /// <summary>Register a new user account.</summary>
    [HttpPost("register")]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RegisterCommand(request.Email, request.Password, request.DisplayName),
            cancellationToken);

        return ToActionResult(result, id => CreatedAtAction(nameof(Register), new { id }, id));
    }

    /// <summary>Log in and receive access + refresh tokens.</summary>
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

        return ToActionResult(await sender.Send(command, cancellationToken));
    }

    /// <summary>Exchange a refresh token for a new token pair.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var command = new RefreshTokenCommand(
            request.Token,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        return ToActionResult(await sender.Send(command, cancellationToken));
    }

    /// <summary>Revoke a refresh token (logout).</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(new LogoutCommand(request.Token), cancellationToken));
}
