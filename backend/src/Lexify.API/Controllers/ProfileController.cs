using Lexify.Application.Abstractions;
using Lexify.Application.UserProfile.Commands.ChangePassword;
using Lexify.Application.UserProfile.Commands.UpdateDisplayName;
using Lexify.Application.UserProfile.Commands.UpdateEnglishLevel;
using Lexify.Application.UserProfile.Commands.UpdateReviewSettings;
using Lexify.Application.UserProfile.Queries.GetProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
}

public sealed record UpdateEnglishLevelRequest(string? EnglishLevel);

public sealed record UpdateReviewSettingsRequest(int NewWordsPerDay);

public sealed record UpdateDisplayNameRequest(string? DisplayName);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
