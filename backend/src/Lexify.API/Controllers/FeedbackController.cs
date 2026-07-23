using Lexify.API.RateLimit;
using Lexify.API.Requests.Feedback;
using Lexify.Application.Abstractions;
using Lexify.Application.Feedbacks.Commands.SubmitFeedback;
using Lexify.Application.Feedbacks.Common;
using Lexify.Application.Feedbacks.Queries.GetMyFeedback;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Lexify.API.Controllers;

/// <summary>
/// User-facing feedback: bug reports, suggestions, reviews and questions. Admin triage of these
/// submissions lives under <c>/api/admin/feedback</c>.
/// </summary>
[Authorize]
[Route("api/feedback")]
public sealed class FeedbackController(ISender sender, ICurrentUserService currentUser) : BaseApiController
{
    /// <summary>Submits a feedback ticket, optionally with screenshots, and returns its ticket code.</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [EnableRateLimiting(FeedbackRateLimiterPolicy.PolicyName)]
    [RequestSizeLimit(AttachmentRules.MaxCount * AttachmentRules.MaxSizeBytes + 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Submit(
        [FromForm] SubmitFeedbackRequest request, CancellationToken cancellationToken)
    {
        var files = request.Attachments ?? [];

        if (files.Count > AttachmentRules.MaxCount)
            return BadRequest(new { message = $"At most {AttachmentRules.MaxCount} attachments are allowed." });

        // Check the declared length before buffering, so an oversized upload never reaches memory.
        if (files.Any(f => f.Length > AttachmentRules.MaxSizeBytes))
            return BadRequest(new
            {
                message = $"Each attachment must be at most {AttachmentRules.MaxSizeBytes / (1024 * 1024)} MB."
            });

        var uploads = new List<FeedbackAttachmentUpload>(files.Count);
        foreach (var file in files)
        {
            using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer, cancellationToken);
            uploads.Add(new FeedbackAttachmentUpload(file.FileName, buffer.ToArray()));
        }

        var command = new SubmitFeedbackCommand(
            currentUser.UserId, request.Type, request.Category, request.Subject,
            request.Message, request.Rating, request.ContactEmail, request.Consent, uploads);

        return ToActionResult(await sender.Send(command, cancellationToken));
    }

    /// <summary>Lists the caller's own submissions with their current triage status, newest first.</summary>
    [HttpGet("mine")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = new GetMyFeedbackQuery(currentUser.UserId, page, pageSize);
        return ToActionResult(await sender.Send(query, cancellationToken));
    }
}
