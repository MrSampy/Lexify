using System.Text.Json;
using FluentValidation;
using Lexify.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Lexify.API.Middleware;

public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    private static readonly Action<ILogger, string, Exception> s_logUnhandled =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(1, "UnhandledException"),
            "Unhandled exception for {Path}");

    private static readonly JsonSerializerOptions s_jsonOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DomainException ex)
        {
            await WriteResponseAsync(
                context,
                StatusCodes.Status400BadRequest,
                new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            await WriteResponseAsync(
                context,
                StatusCodes.Status422UnprocessableEntity,
                new { message = "Validation failed.", errors });
        }
        catch (Exception ex)
        {
            s_logUnhandled(logger, context.Request.Path, ex);
            await WriteResponseAsync(
                context,
                StatusCodes.Status500InternalServerError,
                new { message = "An unexpected error occurred." });
        }
    }

    private static async Task WriteResponseAsync(HttpContext context, int statusCode, object body)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var json = JsonSerializer.Serialize(body, s_jsonOptions);
        await context.Response.WriteAsync(json, context.RequestAborted);
    }
}
