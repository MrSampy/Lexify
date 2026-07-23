using Lexify.Application.Abstractions;
using Lexify.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace Lexify.Infrastructure.Jobs;

public sealed partial class SendEmailChangedNoticeJob(
    IEmailService emailService,
    ILogger<SendEmailChangedNoticeJob> logger)
{
    public async Task RunAsync(string oldEmail, string newEmail, CancellationToken ct = default)
    {
        var html = EmailTemplates.EmailChangedNotice(newEmail);
        await emailService.SendAsync(oldEmail, "Пошту акаунта Lexify змінено", html, ct);
        LogSent(logger, oldEmail);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "SendEmailChangedNoticeJob: change notice sent to former address {Email}")]
    private static partial void LogSent(ILogger logger, string email);
}
