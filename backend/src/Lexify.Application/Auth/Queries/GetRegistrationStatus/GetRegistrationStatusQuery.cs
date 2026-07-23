using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Auth.Queries.GetRegistrationStatus;

public sealed record GetRegistrationStatusQuery : IRequest<Result<RegistrationStatusDto>>;

/// <param name="Open">Anyone may register; no invite code needed.</param>
/// <param name="InviteRequired">Registration is closed but a valid invite code still gets a user in.</param>
/// <param name="EmailVerificationRequired">
/// When true the sign-up form should route to the "check your email" screen; when false the account is
/// auto-confirmed and the user can sign in immediately, so the form should route to login instead.
/// </param>
/// <remarks>Both Open/InviteRequired false means registration is shut outright — the form should say so.</remarks>
public sealed record RegistrationStatusDto(bool Open, bool InviteRequired, bool EmailVerificationRequired);
