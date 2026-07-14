using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Auth.Queries.GetRegistrationStatus;

public sealed record GetRegistrationStatusQuery : IRequest<Result<RegistrationStatusDto>>;

/// <param name="Open">Anyone may register; no invite code needed.</param>
/// <param name="InviteRequired">Registration is closed but a valid invite code still gets a user in.</param>
/// <remarks>Both false means registration is shut outright — the sign-up form should say so.</remarks>
public sealed record RegistrationStatusDto(bool Open, bool InviteRequired);
