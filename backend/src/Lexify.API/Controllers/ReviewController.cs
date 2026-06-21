using Lexify.API.Requests.Review;
using Lexify.Application.Abstractions;
using Lexify.Application.Review.Commands.ReviewWord;
using Lexify.Application.Review.Queries.GetDueForReview;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lexify.API.Controllers;

[Authorize]
[Route("api/review")]
public sealed class ReviewController(ISender sender, ICurrentUserService currentUser) : BaseApiController
{
    /// <summary>Returns up to 20 words due for spaced-repetition review, ordered by next_review_at ASC.</summary>
    [HttpGet("due")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDue(CancellationToken cancellationToken)
    {
        var query = new GetDueForReviewQuery(currentUser.UserId);
        return ToActionResult(await sender.Send(query, cancellationToken));
    }

    /// <summary>Rates a word review (quality 0–5) and updates its SM-2 schedule.</summary>
    [HttpPost("rate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Rate(RateWordRequest request, CancellationToken cancellationToken)
    {
        var command = new ReviewWordCommand(request.WordId, currentUser.UserId, request.Quality);
        return ToActionResult(await sender.Send(command, cancellationToken));
    }
}
