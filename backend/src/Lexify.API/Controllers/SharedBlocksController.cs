using Lexify.Application.Blocks.Commands.CopySharedBlock;
using Lexify.Application.Blocks.Queries.GetSharedBlock;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lexify.API.Controllers;

/// <summary>
/// The recipient's side of a shared block: preview it, then copy it into your own account. Addressed by
/// share token, never by block id — holding the token is the only thing that grants access here, and
/// sign-in is still required so a leaked link can't be crawled anonymously.
/// </summary>
[Authorize]
[Route("api/shared")]
public sealed class SharedBlocksController(ISender sender) : BaseApiController
{
    /// <summary>Read-only preview of the shared block: its words, without the owner's review progress.</summary>
    [HttpGet("{token}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetShared(string token, CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(new GetSharedBlockQuery(token), cancellationToken));

    /// <summary>Copies the shared block into the caller's account and returns the new block's id.</summary>
    [HttpPost("{token}/copy")]
    [ProducesResponseType<Guid>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Copy(string token, CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(new CopySharedBlockCommand(token), cancellationToken));
}
