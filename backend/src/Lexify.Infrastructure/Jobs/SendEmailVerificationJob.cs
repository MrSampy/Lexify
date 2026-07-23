using Lexify.Application.Abstractions;
using Lexify.Domain.Entities;
using Lexify.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Lexify.Infrastructure.Jobs;

public sealed partial class SendEmailVerificationJob(
    IEmailService emailService,
    IConfiguration configuration,
    ILogger<SendEmailVerificationJob> logger)
{
    public async Task RunAsync(string email, string rawToken, string purpose, CancellationToken ct = default)
    {
        var frontendUrl = configuration["Frontend:Url"] ?? "http://localhost:5173";
        var verifyUrl = $"{frontendUrl}/verify-email?token={rawToken}";

        var isChange = purpose == EmailVerificationToken.Purposes.EmailChange;

        var html = isChange
            ? EmailTemplates.EmailChangeVerification(verifyUrl)
            : EmailTemplates.EmailVerification(verifyUrl);

        var subject = isChange
            ? "Lexify — підтвердження нової пошти"
            : "Lexify — підтвердіть свою пошту";

        await emailService.SendAsync(email, subject, html, ct);
        LogSent(logger, email, purpose);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "SendEmailVerificationJob: verification email sent to {Email} ({Purpose})")]
    private static partial void LogSent(ILogger logger, string email, string purpose);
}
