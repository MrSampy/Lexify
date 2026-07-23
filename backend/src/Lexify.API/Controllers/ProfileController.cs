using Lexify.Application.Abstractions;
using Lexify.Application.UserProfile.Commands.ChangePassword;
using Lexify.Application.UserProfile.Commands.ConfirmEnableTwoFactor;
using Lexify.Application.UserProfile.Commands.DisableTwoFactor;
using Lexify.Application.UserProfile.Commands.EnableTwoFactor;
using Lexify.Application.UserProfile.Commands.RequestEmailChange;
using Lexify.Application.UserProfile.Commands.UpdateDisplayName;
using Lexify.Application.UserProfile.Commands.UpdateEnglishLevel;
using Lexify.Application.UserProfile.Commands.UpdateReviewSettings;
using Lexify.Application.UserProfile.Queries.GetProfile;
using Lexify.API.RateLimit;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Lexify.API.Controllers;

[Authorize]
[Route("api/profile")]
public sealed class ProfileController(ISender sender, ICurrentUserService currentUser) : BaseApiController
{
    /// <summary>Returns the current user's profile.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfile(CancellationToken ct) =>
        ToActionResult(await sender.Send(new GetProfileQuery(currentUser.UserId), ct));

    /// <summary>Sets the current user's CEFR English level (A1..C2, or null to clear).</summary>
    [HttpPut("english-level")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateEnglishLevel(
        UpdateEnglishLevelRequest request, CancellationToken ct) =>
        ToActionResult(await sender.Send(
            new UpdateEnglishLevelCommand(currentUser.UserId, request.EnglishLevel), ct));

    /// <summary>Updates the current user's display name (null/empty clears it).</summary>
    [HttpPut("display-name")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateDisplayName(
        UpdateDisplayNameRequest request, CancellationToken ct) =>
        ToActionResult(await sender.Send(
            new UpdateDisplayNameCommand(currentUser.UserId, request.DisplayName), ct));

    /// <summary>Sets how many new (never-reviewed) words enter the review queue per day (0–100).</summary>
    [HttpPut("review-settings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateReviewSettings(
        UpdateReviewSettingsRequest request, CancellationToken ct) =>
        ToActionResult(await sender.Send(
            new UpdateReviewSettingsCommand(currentUser.UserId, request.NewWordsPerDay), ct));

    /// <summary>Changes the current user's password; all other sessions are revoked.</summary>
    [HttpPut("password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordRequest request, CancellationToken ct) =>
        ToActionResult(await sender.Send(
            new ChangePasswordCommand(currentUser.UserId, request.CurrentPassword, request.NewPassword), ct));

    /// <summary>
    /// Starts an email change: sends a confirmation link to the new address. The account keeps its
    /// current address until that link is opened.
    /// </summary>
    [HttpPut("email")]
    [EnableRateLimiting(AccountEmailRateLimiterPolicy.PolicyName)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> RequestEmailChange(
        RequestEmailChangeRequest request, CancellationToken ct) =>
        ToActionResult(await sender.Send(
            new RequestEmailChangeCommand(currentUser.UserId, request.NewEmail, request.CurrentPassword), ct));

    /// <summary>Starts opting into 2FA: emails a confirmation code (the flag flips only on confirm).</summary>
    [HttpPost("2fa/enable")]
    [EnableRateLimiting(AccountEmailRateLimiterPolicy.PolicyName)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> EnableTwoFactor(CancellationToken ct) =>
        ToActionResult(await sender.Send(new EnableTwoFactorCommand(currentUser.UserId), ct));

    /// <summary>Confirms the emailed code and turns 2FA on for the account.</summary>
    [HttpPost("2fa/confirm")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ConfirmTwoFactor(
        ConfirmTwoFactorRequest request, CancellationToken ct) =>
        ToActionResult(await sender.Send(
            new ConfirmEnableTwoFactorCommand(currentUser.UserId, request.Code), ct));

    /// <summary>Re-sends the enrollment code while opting in.</summary>
    [HttpPost("2fa/resend")]
    [EnableRateLimiting(AccountEmailRateLimiterPolicy.PolicyName)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResendEnableTwoFactor(CancellationToken ct) =>
        ToActionResult(await sender.Send(new EnableTwoFactorCommand(currentUser.UserId), ct));

    /// <summary>Turns 2FA off (re-authenticated with the current password). Blocked for admins.</summary>
    [HttpDelete("2fa")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DisableTwoFactor(
        DisableTwoFactorRequest request, CancellationToken ct) =>
        ToActionResult(await sender.Send(
            new DisableTwoFactorCommand(currentUser.UserId, request.CurrentPassword), ct));
}

public sealed record UpdateEnglishLevelRequest(string? EnglishLevel);

public sealed record UpdateReviewSettingsRequest(int NewWordsPerDay);

public sealed record UpdateDisplayNameRequest(string? DisplayName);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record RequestEmailChangeRequest(string NewEmail, string CurrentPassword);

public sealed record ConfirmTwoFactorRequest(string Code);

public sealed record DisableTwoFactorRequest(string CurrentPassword);
