using Lexify.Application.Abstractions;
using Lexify.Application.Tags.Queries.GetUserTags;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lexify.API.Controllers;

[Authorize]
[Route("api/tags")]
public sealed class TagsController(ISender sender, ICurrentUserService currentUser) : BaseApiController
{
    /// <summary>Returns all tag names belonging to the current user.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserTags(CancellationToken cancellationToken = default)
    {
        var query = new GetUserTagsQuery(currentUser.UserId);
        return ToActionResult(await sender.Send(query, cancellationToken));
    }
}
