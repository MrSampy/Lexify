using System.Security.Cryptography;
using System.Text;
using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Auth.Commands.Logout;

public sealed class LogoutCommandHandler(IRefreshTokenRepository refreshTokenRepository)
    : IRequestHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));
        var token = await refreshTokenRepository.GetByHashAsync(hash, cancellationToken);

        if (token is null || !token.IsActive)
            return Result.Ok();

        token.Revoke();
        await refreshTokenRepository.UpdateAsync(token, cancellationToken);

        return Result.Ok();
    }
}
