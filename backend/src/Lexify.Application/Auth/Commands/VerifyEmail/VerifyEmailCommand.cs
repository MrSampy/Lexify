using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Auth.Commands.VerifyEmail;

public sealed record VerifyEmailCommand(string Token) : IRequest<Result<VerifyEmailResultDto>>;

/// <param name="EmailChanged">True when the link completed an address change rather than a sign-up.</param>
public sealed record VerifyEmailResultDto(string Email, bool EmailChanged);
