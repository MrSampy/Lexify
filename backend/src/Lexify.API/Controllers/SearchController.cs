using Lexify.Application.Abstractions;
using Lexify.Application.Words.Queries.SearchWords;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lexify.API.Controllers;

[Authorize]
[Route("api/search")]
public sealed class SearchController(ISender sender, ICurrentUserService currentUser) : BaseApiController
{
    /// <summary>Full-text search across the current user's words.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<SearchWordDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] short? lang = null,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new SearchWordsQuery(currentUser.UserId, q ?? "", lang, Math.Min(limit, 50));
        return ToActionResult(await sender.Send(query, cancellationToken));
    }
}
