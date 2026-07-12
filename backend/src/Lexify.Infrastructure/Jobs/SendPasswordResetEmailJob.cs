using Lexify.Application.Abstractions;
using Lexify.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Lexify.Infrastructure.Jobs;

public sealed partial class SendPasswordResetEmailJob(
    IEmailService emailService,
    IConfiguration configuration,
    ILogger<SendPasswordResetEmailJob> logger)
{
    public async Task RunAsync(string email, string rawToken, CancellationToken ct = default)
    {
        var frontendUrl = configuration["Frontend:Url"] ?? "http://localhost:5173";
        var resetUrl = $"{frontendUrl}/reset-password?token={rawToken}";

        var html = EmailTemplates.PasswordReset(resetUrl);
        await emailService.SendAsync(email, "Lexify — скидання паролю", html, ct);
        LogSent(logger, email);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "SendPasswordResetEmailJob: reset email sent to {Email}")]
    private static partial void LogSent(ILogger logger, string email);
}
