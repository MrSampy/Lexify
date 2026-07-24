using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.UserProfile.Queries.GetProfile;

public sealed class GetProfileQueryHandler(
    IUserRepository userRepository,
    IEmailVerificationTokenRepository verificationTokenRepository)
    : IRequestHandler<GetProfileQuery, Result<ProfileDto>>
{
    public async Task<Result<ProfileDto>> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.NotFound<ProfileDto>("User not found.");

        // Surfaced so the profile can say "waiting on confirmation at <address>" instead of looking
        // like the change silently failed.
        var pending = await verificationTokenRepository.GetActiveEmailChangeAsync(
            user.Id, cancellationToken);

        return Result.Ok(new ProfileDto(
            user.Email, user.DisplayName, user.EnglishLevel, user.NewWordsPerDay,
            user.IsEmailVerified, pending?.NewEmail,
            user.TwoFactorEnabled, user.IsTwoFactorMandatory,
            user.EmailRemindersEnabled));
    }
}
