using Lexify.Application.Abstractions;
using Lexify.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace Lexify.Infrastructure.Jobs;

public sealed partial class SendWelcomeEmailJob(
    IEmailService emailService,
    ILogger<SendWelcomeEmailJob> logger)
{
    public async Task RunAsync(string email, string username, CancellationToken ct = default)
    {
        var html = EmailTemplates.Welcome(username);
        await emailService.SendAsync(email, "Ласкаво просимо до Lexify!", html, ct);
        LogSent(logger, email);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "SendWelcomeEmailJob: welcome email sent to {Email}")]
    private static partial void LogSent(ILogger logger, string email);
}
