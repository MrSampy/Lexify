using System.Security.Cryptography;
using System.Text;
using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Auth.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordResetTokenRepository passwordResetTokenRepository,
    IBackgroundJobService backgroundJobService)
    : IRequestHandler<ForgotPasswordCommand, Result>
{
    private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromHours(1);

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        // Always succeed: the response must not reveal whether the email is registered.
        if (user is null || user.Status != User.Statuses.Active)
            return Result.Ok();

        // A new request supersedes any previously issued links.
        await passwordResetTokenRepository.InvalidateActiveForUserAsync(user.Id, cancellationToken);

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        var token = new PasswordResetToken(
            user.Id,
            tokenHash,
            DateTimeOffset.UtcNow.Add(ResetTokenLifetime));

        await passwordResetTokenRepository.AddAsync(token, cancellationToken);

        backgroundJobService.EnqueuePasswordResetEmail(user.Email, rawToken);

        return Result.Ok();
    }
}
