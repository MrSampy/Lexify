using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Notifications.Commands.Unsubscribe;

/// <summary>
/// Turns off reminder emails for whoever the link was minted for. Runs unauthenticated — the signature
/// on the token is the proof — so it must not become an oracle: a bad token and a deleted account are
/// reported the same way, and unsubscribing twice succeeds twice.
/// </summary>
public sealed class UnsubscribeCommandHandler(
    IUnsubscribeTokenService tokenService,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UnsubscribeCommand, Result>
{
    public async Task<Result> Handle(UnsubscribeCommand request, CancellationToken cancellationToken)
    {
        if (!tokenService.TryValidate(request.Token, out var userId))
            return Result.Failure("This unsubscribe link is not valid.");

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result.Failure("This unsubscribe link is not valid.");

        user.SetEmailReminders(false);
        await userRepository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
