using Lexify.Application.Notifications.Commands.Unsubscribe;
using Lexify.API.RateLimit;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Lexify.API.Controllers;

[Route("api/notifications")]
public sealed class NotificationsController(ISender sender) : BaseApiController
{
    /// <summary>
    /// Opts the account out of reminder emails using the signed token from the mail's unsubscribe link.
    /// Anonymous by necessity — someone unsubscribing is usually not signed in — with the token's
    /// signature standing in for authentication.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("unsubscribe")]
    [EnableRateLimiting(AuthRateLimiterPolicy.PolicyName)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Unsubscribe(
        UnsubscribeRequest request, CancellationToken ct) =>
        ToActionResult(await sender.Send(new UnsubscribeCommand(request.Token), ct));
}

public sealed record UnsubscribeRequest(string Token);
