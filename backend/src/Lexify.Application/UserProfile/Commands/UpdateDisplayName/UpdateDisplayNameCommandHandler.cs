using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.UserProfile.Commands.UpdateDisplayName;

public sealed class UpdateDisplayNameCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateDisplayNameCommand, Result>
{
    public async Task<Result> Handle(UpdateDisplayNameCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.NotFound("User not found.");

        user.UpdateDisplayName(request.DisplayName);
        await userRepository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
