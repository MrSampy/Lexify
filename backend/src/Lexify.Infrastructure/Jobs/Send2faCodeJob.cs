using Lexify.Application.Abstractions;
using Lexify.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace Lexify.Infrastructure.Jobs;

public sealed partial class Send2faCodeJob(
    IEmailService emailService,
    ILogger<Send2faCodeJob> logger)
{
    public async Task RunAsync(string email, string code, CancellationToken ct = default)
    {
        var html = EmailTemplates.TwoFactorCode(code);
        await emailService.SendAsync(email, "Lexify — код підтвердження входу", html, ct);
        LogSent(logger, email);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Send2faCodeJob: sign-in code sent to {Email}")]
    private static partial void LogSent(ILogger logger, string email);
}
