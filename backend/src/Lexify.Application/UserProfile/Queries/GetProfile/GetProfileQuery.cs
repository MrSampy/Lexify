using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.UserProfile.Queries.GetProfile;

public sealed record GetProfileQuery(Guid UserId) : IRequest<Result<ProfileDto>>;

public sealed record ProfileDto(
    string Email,
    string? DisplayName,
    string? EnglishLevel,
    int NewWordsPerDay);
