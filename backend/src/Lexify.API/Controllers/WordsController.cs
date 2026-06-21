using Lexify.API.Requests.Words;
using Lexify.Application.Abstractions;
using Lexify.Application.Words.Commands.CreateWord;
using Lexify.Application.Words.Commands.DeleteWord;
using Lexify.Application.Words.Commands.ImportWords;
using Lexify.Application.Words.Commands.UpdateWord;
using Lexify.Application.Words.Queries.GetWordsByBlock;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lexify.API.Controllers;

[Authorize]
[Route("api/blocks/{blockId:guid}/words")]
public sealed class WordsController(ISender sender, ICurrentUserService currentUser) : BaseApiController
{
    /// <summary>Returns a paginated, searchable list of words in the block.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWords(
        Guid blockId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new GetWordsByBlockQuery(blockId, currentUser.UserId, search, page, pageSize);
        return ToActionResult(await sender.Send(query, cancellationToken));
    }

    /// <summary>Adds a single word to the block.</summary>
    [HttpPost]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateWord(
        Guid blockId,
        CreateWordRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateWordCommand(
            blockId,
            request.Term,
            request.Translation,
            request.WordType,
            request.Notes,
            request.ExampleSentence,
            request.SortOrder);

        var result = await sender.Send(command, cancellationToken);
        return ToActionResult(result, id => CreatedAtAction(nameof(GetWords), new { blockId }, id));
    }

    /// <summary>Updates translation, notes, and confidence flags for a word.</summary>
    [HttpPatch("{wordId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateWord(
        Guid wordId,
        UpdateWordRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateWordCommand(
            wordId,
            request.Translation,
            request.Notes,
            request.ExampleSentence,
            request.ConfidenceFlag,
            request.ConfidenceNote);

        return ToActionResult(await sender.Send(command, cancellationToken));
    }

    /// <summary>Deletes a word from the block.</summary>
    [HttpDelete("{wordId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWord(
        Guid wordId,
        CancellationToken cancellationToken)
    {
        return ToActionResult(await sender.Send(new DeleteWordCommand(wordId), cancellationToken));
    }

    /// <summary>Bulk-imports up to 200 pre-formatted words into the block.</summary>
    [HttpPost("import")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ImportWords(
        Guid blockId,
        ImportWordsRequest request,
        CancellationToken cancellationToken)
    {
        var items = request.Words
            .Select(w => new ImportWordItem(
                w.Term, w.Translation, w.WordType,
                w.Notes, w.ExampleSentence,
                w.ConfidenceFlag, w.ConfidenceNote, w.SortOrder))
            .ToList();

        var command = new ImportWordsCommand(blockId, items);
        return ToActionResult(await sender.Send(command, cancellationToken));
    }
}
