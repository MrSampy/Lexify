using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.UserProfile.Commands.UpdateNotificationSettings;

public sealed class UpdateNotificationSettingsCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateNotificationSettingsCommand, Result>
{
    public async Task<Result> Handle(
        UpdateNotificationSettingsCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.NotFound("User not found.");

        user.SetEmailReminders(request.EmailRemindersEnabled);
        await userRepository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
