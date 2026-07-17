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
    // Practice ("cram") sessions return the whole scope regardless of schedule, so they need a much
    // larger cap than the ~20 due words a normal session surfaces.
    private const int DueLimit = 20;
    private const int CramLimit = 500;

    /// <summary>
    /// Words for a review session. Optionally scoped to <paramref name="blockId"/>; pass
    /// <c>mode=cram</c> to practise every word in scope regardless of its due date.
    /// </summary>
    [HttpGet("due")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDue(
        [FromQuery] Guid? blockId, [FromQuery] string? mode, CancellationToken cancellationToken)
    {
        var cram = string.Equals(mode, "cram", StringComparison.OrdinalIgnoreCase);
        var query = new GetDueForReviewQuery(
            currentUser.UserId, cram ? CramLimit : DueLimit, blockId, cram);
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
