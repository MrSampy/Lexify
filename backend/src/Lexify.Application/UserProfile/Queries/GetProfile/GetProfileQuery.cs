using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.UserProfile.Queries.GetProfile;

public sealed record GetProfileQuery(Guid UserId) : IRequest<Result<ProfileDto>>;

/// <param name="PendingEmail">Address awaiting confirmation from an in-flight change; null when none.</param>
/// <param name="TwoFactorEnabled">The user's opt-in flag (admins are forced on regardless).</param>
/// <param name="TwoFactorMandatory">True for admins — the UI shows 2FA as locked-on, no toggle.</param>
public sealed record ProfileDto(
    string Email,
    string? DisplayName,
    string? EnglishLevel,
    int NewWordsPerDay,
    bool EmailVerified,
    string? PendingEmail,
    bool TwoFactorEnabled,
    bool TwoFactorMandatory);
