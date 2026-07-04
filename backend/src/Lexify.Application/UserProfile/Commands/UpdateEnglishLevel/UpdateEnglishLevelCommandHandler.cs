using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.UserProfile.Commands.UpdateEnglishLevel;

public sealed class UpdateEnglishLevelCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateEnglishLevelCommand, Result>
{
    public async Task<Result> Handle(UpdateEnglishLevelCommand request, CancellationToken cancellationToken)
    {
        if (request.EnglishLevel is not null && !User.EnglishLevels.All.Contains(request.EnglishLevel))
            return Result.Failure($"Invalid English level '{request.EnglishLevel}'.");

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.NotFound("User not found.");

        user.SetEnglishLevel(request.EnglishLevel);
        await userRepository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
