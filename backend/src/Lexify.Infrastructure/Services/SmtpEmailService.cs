using Lexify.Application.Abstractions;
using Lexify.Infrastructure.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace Lexify.Infrastructure.Services;

public sealed partial class SmtpEmailService(
    IOptions<SmtpSettings> opts,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    public async Task SendAsync(
        string recipient, string subject, string body, CancellationToken cancellationToken = default)
    {
        var settings = opts.Value;

        if (string.IsNullOrWhiteSpace(settings.Host))
        {
            LogSmtpNotConfigured(logger, recipient);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.FromName, settings.FromAddress));
        message.To.Add(MailboxAddress.Parse(recipient));
        message.Subject = subject;
        message.Body = new TextPart(TextFormat.Html) { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(
            settings.Host, settings.Port,
            settings.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
            cancellationToken);

        if (!string.IsNullOrEmpty(settings.Username))
            await client.AuthenticateAsync(settings.Username, settings.Password, cancellationToken);

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);

        LogSent(logger, recipient, subject);
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "SmtpEmailService: SMTP not configured, skipping email to {Recipient}")]
    private static partial void LogSmtpNotConfigured(ILogger logger, string recipient);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "SmtpEmailService: sent email to {Recipient} — {Subject}")]
    private static partial void LogSent(ILogger logger, string recipient, string subject);
}
