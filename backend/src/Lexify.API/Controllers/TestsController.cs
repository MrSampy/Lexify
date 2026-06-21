using Lexify.API.Requests.Tests;
using Lexify.Application.Abstractions;
using Lexify.Application.Tests.Commands.DeleteTest;
using Lexify.Application.Tests.Commands.GenerateTest;
using Lexify.Application.Tests.Queries.GetTestById;
using Lexify.Application.Tests.Queries.GetTests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lexify.API.Controllers;

[Authorize]
[Route("api/tests")]
public sealed class TestsController(ISender sender, ICurrentUserService currentUser) : BaseApiController
{
    /// <summary>Returns a paginated list of the current user's tests.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTests(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetTestsQuery(currentUser.UserId, status, page, pageSize);
        return ToActionResult(await sender.Send(query, cancellationToken));
    }

    /// <summary>Returns a single test with its questions (without correct answers).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTest(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetTestByIdQuery(id, currentUser.UserId);
        return ToActionResult(await sender.Send(query, cancellationToken));
    }

    /// <summary>Starts AI test generation and returns the test ID immediately (status: generating).</summary>
    [HttpPost("generate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GenerateTest(
        GenerateTestRequest request,
        CancellationToken cancellationToken)
    {
        var command = new GenerateTestCommand(
            currentUser.UserId,
            request.BlockIds,
            request.QuestionTypes,
            request.QuestionCount);

        return ToActionResult(await sender.Send(command, cancellationToken));
    }

    /// <summary>Archives (soft-deletes) a test.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTest(Guid id, CancellationToken cancellationToken)
    {
        return ToActionResult(await sender.Send(new DeleteTestCommand(id, currentUser.UserId), cancellationToken));
    }
}
