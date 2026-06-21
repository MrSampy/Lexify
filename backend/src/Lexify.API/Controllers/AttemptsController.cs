using Lexify.API.Requests.Tests;
using Lexify.Application.Abstractions;
using Lexify.Application.Tests.Commands.FinishAttempt;
using Lexify.Application.Tests.Commands.StartAttempt;
using Lexify.Application.Tests.Commands.SubmitAnswer;
using Lexify.Application.Tests.Queries.GetAttemptResults;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lexify.API.Controllers;

[Authorize]
public sealed class AttemptsController(ISender sender, ICurrentUserService currentUser) : BaseApiController
{
    /// <summary>Starts a new attempt for the given test.</summary>
    [HttpPost("api/tests/{testId:guid}/attempts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartAttempt(Guid testId, CancellationToken cancellationToken)
    {
        var command = new StartAttemptCommand(testId, currentUser.UserId);
        return ToActionResult(await sender.Send(command, cancellationToken));
    }

    /// <summary>Submits an answer for one question and returns immediate feedback.</summary>
    [HttpPost("api/attempts/{id:guid}/answer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitAnswer(
        Guid id,
        SubmitAnswerRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SubmitAnswerCommand(
            id, request.QuestionId, currentUser.UserId, request.GivenAnswer, request.TimeSpentMs);
        return ToActionResult(await sender.Send(command, cancellationToken));
    }

    /// <summary>Finishes the attempt, computes the score, and applies SM-2 penalties for wrong answers.</summary>
    [HttpPost("api/attempts/{id:guid}/finish")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> FinishAttempt(Guid id, CancellationToken cancellationToken)
    {
        return ToActionResult(await sender.Send(new FinishAttemptCommand(id, currentUser.UserId), cancellationToken));
    }

    /// <summary>Returns the full results of a finished attempt, including correct answers.</summary>
    [HttpGet("api/attempts/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAttemptResults(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetAttemptResultsQuery(id, currentUser.UserId);
        return ToActionResult(await sender.Send(query, cancellationToken));
    }
}
