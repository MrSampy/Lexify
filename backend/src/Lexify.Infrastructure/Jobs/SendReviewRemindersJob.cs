using Lexify.Application.Abstractions;
using Lexify.Domain.Repositories;
using Lexify.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Lexify.Infrastructure.Jobs;

public sealed partial class SendReviewRemindersJob(
    IUserRepository userRepository,
    IEmailService emailService,
    IUnsubscribeTokenService unsubscribeTokens,
    IConfiguration configuration,
    ILogger<SendReviewRemindersJob> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        var frontendUrl = configuration["Frontend:Url"] ?? "http://localhost:5173";
        var users = await userRepository.GetUsersWithDueWordsAsync(ct);

        if (users.Count == 0)
        {
            LogNoReminders(logger);
            return;
        }

        int sent = 0;
        foreach (var (userId, email, count) in users)
        {
            try
            {
                var username = email.Split('@')[0];
                var unsubscribeUrl =
                    $"{frontendUrl}/unsubscribe?token={Uri.EscapeDataString(unsubscribeTokens.Create(userId))}";
                var html = EmailTemplates.ReviewReminder(username, count, frontendUrl, unsubscribeUrl);
                await emailService.SendAsync(email, "Lexify — час повторити слова!", html, ct);
                sent++;
            }
            catch (Exception ex)
            {
                LogSendFailed(logger, ex, email);
            }
        }

        LogCompleted(logger, sent, users.Count);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "SendReviewRemindersJob: no users with due words")]
    private static partial void LogNoReminders(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "SendReviewRemindersJob: failed to send reminder to {Email}")]
    private static partial void LogSendFailed(ILogger logger, Exception ex, string email);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "SendReviewRemindersJob: sent {Sent}/{Total} reminders")]
    private static partial void LogCompleted(ILogger logger, int sent, int total);
}
