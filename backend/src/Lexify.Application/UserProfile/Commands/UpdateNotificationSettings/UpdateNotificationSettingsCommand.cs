using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.UserProfile.Commands.UpdateNotificationSettings;

/// <param name="EmailRemindersEnabled">False turns off the daily "words are due" email.</param>
public sealed record UpdateNotificationSettingsCommand(Guid UserId, bool EmailRemindersEnabled)
    : IRequest<Result>;
