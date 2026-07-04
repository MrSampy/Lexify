using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.UserProfile.Queries.GetProfile;

public sealed class GetProfileQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetProfileQuery, Result<ProfileDto>>
{
    public async Task<Result<ProfileDto>> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.NotFound<ProfileDto>("User not found.");

        return Result.Ok(new ProfileDto(user.Email, user.DisplayName, user.EnglishLevel));
    }
}
