using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexify.Application.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly Action<ILogger, string, Exception?> s_logHandling =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(1, "Handling"),
            "Handling {RequestName}");

    private static readonly Action<ILogger, string, long, Exception?> s_logHandled =
        LoggerMessage.Define<string, long>(
            LogLevel.Information,
            new EventId(2, "Handled"),
            "Handled {RequestName} in {ElapsedMs}ms");

    private static readonly Action<ILogger, string, long, Exception?> s_logError =
        LoggerMessage.Define<string, long>(
            LogLevel.Error,
            new EventId(3, "HandlingError"),
            "Error handling {RequestName} after {ElapsedMs}ms");

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        s_logHandling(logger, requestName, null);

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await next(cancellationToken);
            sw.Stop();
            s_logHandled(logger, requestName, sw.ElapsedMilliseconds, null);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            s_logError(logger, requestName, sw.ElapsedMilliseconds, ex);
            throw;
        }
    }
}
