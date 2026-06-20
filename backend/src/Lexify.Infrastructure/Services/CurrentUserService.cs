using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Lexify.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Lexify.Infrastructure.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid UserId
    {
        get
        {
            var sub = httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
        }
    }

    public string Email =>
        httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Email)?.Value ?? string.Empty;

    public string Role =>
        httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
