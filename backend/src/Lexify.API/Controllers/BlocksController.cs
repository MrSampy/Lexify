using Lexify.API.Requests.Blocks;
using Lexify.Application.Abstractions;
using Lexify.Application.Blocks.Commands.AddTagToBlock;
using Lexify.Application.Blocks.Commands.CreateBlock;
using Lexify.Application.Blocks.Commands.CreateBlockShare;
using Lexify.Application.Blocks.Commands.DeleteBlock;
using Lexify.Application.Blocks.Commands.ExportBlock;
using Lexify.Application.Blocks.Commands.ImportBlockFromCsv;
using Lexify.Application.Blocks.Commands.RemoveTagFromBlock;
using Lexify.Application.Blocks.Commands.RevokeBlockShare;
using Lexify.Application.Blocks.Commands.UpdateBlock;
using Lexify.Application.Blocks.Queries.GetBlockById;
using Lexify.Application.Blocks.Queries.GetBlocks;
using Lexify.Application.Blocks.Queries.GetBlockShare;
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

    /// <summary>Adds a tag to a block. Creates the tag if it doesn't exist for this user.</summary>
    [HttpPost("{id:guid}/tags")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddTag(
        Guid id,
        AddTagRequest request,
        CancellationToken cancellationToken)
    {
        return ToActionResult(await sender.Send(new AddTagToBlockCommand(id, request.TagName), cancellationToken));
    }

    /// <summary>Removes a tag from a block.</summary>
    [HttpDelete("{id:guid}/tags/{tagName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveTag(
        Guid id,
        string tagName,
        CancellationToken cancellationToken)
    {
        return ToActionResult(await sender.Send(new RemoveTagFromBlockCommand(id, tagName), cancellationToken));
    }

    /// <summary>Returns the block's active share link, or null when sharing is off.</summary>
    [HttpGet("{id:guid}/share")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetShare(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(new GetBlockShareQuery(id), cancellationToken));

    /// <summary>Turns sharing on and returns the link. Calling it again returns the same link.</summary>
    [HttpPost("{id:guid}/share")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateShare(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(new CreateBlockShareCommand(id), cancellationToken));

    /// <summary>Turns sharing off — the link stops working. Copies others already made are untouched.</summary>
    [HttpDelete("{id:guid}/share")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeShare(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(new RevokeBlockShareCommand(id), cancellationToken));

    /// <summary>Exports a block and all its words as a CSV file.</summary>
    [HttpGet("{id:guid}/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportBlock(
        Guid id,
        [FromQuery] string format = "csv",
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ExportBlockCommand(id, currentUser.UserId), cancellationToken);
        if (!result.IsSuccess) return ToActionResult(result);
        var r = result.Value!;
        return File(r.Content, r.ContentType, r.FileName);
    }

    /// <summary>Creates a new block by importing words from a CSV file.</summary>
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ImportBlockFromCsv(
        [FromForm] ImportBlockRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File.Length > 5_242_880)
            return BadRequest(new { message = "File too large (max 5 MB)." });

        using var reader = new System.IO.StreamReader(request.File.OpenReadStream());
        var csvContent = await reader.ReadToEndAsync(cancellationToken);

        var command = new ImportBlockFromCsvCommand(request.Title, request.LanguageId, csvContent);
        var result = await sender.Send(command, cancellationToken);
        return ToActionResult(result, id => CreatedAtAction(nameof(GetBlock), new { id }, id));
    }
}
