using Lexify.Application.Abstractions;
using Lexify.Application.Auth.Common;
using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Auth.Commands.Login;

public sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IEmailVerificationService emailVerification,
    ITwoFactorService twoFactor,
    IJwtService jwtService,
    IAuthSessionService authSession)
    : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        // Same message for both "not found" and "wrong password" to prevent user enumeration.
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result.Failure<LoginResponse>("Invalid credentials.");

        if (user.Status != User.Statuses.Active)
            return Result.Failure<LoginResponse>("Account is not active.");

        // Checked after the password so an attacker can't use this branch to discover which addresses
        // are registered — you only reach it by already knowing the credentials.
        if (!user.IsEmailVerified && await emailVerification.IsRequiredAsync(cancellationToken))
            return Result.Failure<LoginResponse>(AuthErrorCodes.EmailNotVerified);

        // Second factor: also gated behind the password check, so it never leaks whether 2FA is on for an
        // address. Instead of a session we hand back a short-lived challenge and email the code; the client
        // completes login/verify-2fa to exchange the challenge + code for a real session.
        if (await twoFactor.IsRequiredForAsync(user, cancellationToken))
        {
            await twoFactor.IssueCodeAsync(user, cancellationToken);
            var challenge = jwtService.GenerateTwoFactorChallengeToken(user.Id);
            return Result.Ok(LoginResponse.Challenge(challenge));
        }

        var session = await authSession.IssueAsync(
            user, request.IpAddress, request.UserAgent, cancellationToken);
        return Result.Ok(LoginResponse.Authenticated(session));
    }
}
