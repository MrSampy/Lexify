namespace Lexify.Application.Auth.Commands.Common;

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt);
