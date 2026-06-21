using Lexify.API.Requests.Blocks;
using Lexify.Application.Abstractions;
using Lexify.Application.Blocks.Commands.CreateBlock;
using Lexify.Application.Blocks.Commands.DeleteBlock;
using Lexify.Application.Blocks.Commands.UpdateBlock;
using Lexify.Application.Blocks.Queries.GetBlockById;
using Lexify.Application.Blocks.Queries.GetBlocks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lexify.API.Controllers;

[Authorize]
[Route("api/blocks")]
public sealed class BlocksController(ISender sender, ICurrentUserService currentUser) : BaseApiController
{
    /// <summary>Returns a paginated list of the current user's blocks.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBlocks(
        [FromQuery] short? languageId,
        [FromQuery] string? tag,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetBlocksQuery(currentUser.UserId, languageId, tag, page, pageSize);
        return ToActionResult(await sender.Send(query, cancellationToken));
    }

    /// <summary>Returns a single block with its paginated word list.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBlock(
        Guid id,
        [FromQuery] int wordsPage = 1,
        [FromQuery] int wordsPageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new GetBlockByIdQuery(id, currentUser.UserId, wordsPage, wordsPageSize);
        return ToActionResult(await sender.Send(query, cancellationToken));
    }

    /// <summary>Creates a new word block.</summary>
    [HttpPost]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateBlock(
        CreateBlockRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateBlockCommand(request.LanguageId, request.Title, request.Description);
        var result = await sender.Send(command, cancellationToken);
        return ToActionResult(result, id => CreatedAtAction(nameof(GetBlock), new { id }, id));
    }

    /// <summary>Updates the title and description of a block.</summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateBlock(
        Guid id,
        UpdateBlockRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateBlockCommand(id, request.Title, request.Description);
        return ToActionResult(await sender.Send(command, cancellationToken));
    }

    /// <summary>Deletes a block and all its words.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBlock(Guid id, CancellationToken cancellationToken)
    {
        return ToActionResult(await sender.Send(new DeleteBlockCommand(id), cancellationToken));
    }
}
