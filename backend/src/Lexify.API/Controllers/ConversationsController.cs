using System.Text.Json;
using Lexify.API.RateLimit;
using Lexify.API.Requests.Conversations;
using Lexify.Application.Abstractions;
using Lexify.Application.Conversations.Commands.EndConversation;
using Lexify.Application.Conversations.Commands.SendMessage;
using Lexify.Application.Conversations.Commands.StartConversation;
using Lexify.Application.Conversations.Queries.GetConversationById;
using Lexify.Application.Conversations.Queries.GetConversations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Lexify.API.Controllers;

/// <summary>
/// "Talk to Lexi" — AI conversation practice. The learner chats in the language they're studying while
/// Lexi steers the words they need to review into the conversation; ending a session feeds SM-2.
/// </summary>
[Authorize]
[Route("api/conversations")]
public sealed class ConversationsController(ISender sender, ICurrentUserService currentUser) : BaseApiController
{
    private static readonly JsonSerializerOptions SseJsonOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>Starts a practice conversation and returns Lexi's opening line and the target words.</summary>
    [HttpPost]
    [EnableRateLimiting(AiRateLimiterPolicy.PolicyName)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Start(
        StartConversationRequest request, CancellationToken cancellationToken)
    {
        var command = new StartConversationCommand(
            currentUser.UserId, request.BlockId, request.Scenario, request.NativeLanguage);
        return ToActionResult(await sender.Send(command, cancellationToken));
    }

    /// <summary>Sends a learner message and streams Lexi's reply as SSE (streaming → done/error).</summary>
    [HttpPost("{id:guid}/messages")]
    [EnableRateLimiting(AiRateLimiterPolicy.PolicyName)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task SendMessage(
        Guid id, [FromBody] SendConversationMessageRequest request, CancellationToken cancellationToken)
    {
        var command = new SendConversationMessageCommand(
            id, currentUser.UserId, request.Message, request.NativeLanguage);

        HttpContext.Response.Headers["Content-Type"] = "text/event-stream";
        HttpContext.Response.Headers["Cache-Control"] = "no-cache";
        HttpContext.Response.Headers["X-Accel-Buffering"] = "no";

        await foreach (var evt in sender.CreateStream(command, cancellationToken))
        {
            string data = evt.EventType switch
            {
                "streaming" => JsonSerializer.Serialize(new { chunk = evt.Chunk }, SseJsonOptions),
                "error"     => JsonSerializer.Serialize(new { message = evt.ErrorMessage }, SseJsonOptions),
                _           => "{}"
            };

            await HttpContext.Response.WriteAsync($"event: {evt.EventType}\ndata: {data}\n\n", cancellationToken);
            await HttpContext.Response.Body.FlushAsync(cancellationToken);
        }
    }

    /// <summary>Ends a conversation, analyses word usage, and returns per-word SM-2 outcomes.</summary>
    [HttpPost("{id:guid}/end")]
    [EnableRateLimiting(AiRateLimiterPolicy.PolicyName)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> End(Guid id, CancellationToken cancellationToken)
    {
        var command = new EndConversationCommand(id, currentUser.UserId);
        return ToActionResult(await sender.Send(command, cancellationToken));
    }

    /// <summary>Lists the user's conversations, newest first.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConversations(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = new GetConversationsQuery(currentUser.UserId, page, pageSize);
        return ToActionResult(await sender.Send(query, cancellationToken));
    }

    /// <summary>Returns one conversation with its full transcript.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConversation(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetConversationByIdQuery(id, currentUser.UserId);
        return ToActionResult(await sender.Send(query, cancellationToken));
    }
}
