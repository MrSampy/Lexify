namespace Lexify.Application.Auth.Commands.Common;

/// <param name="RefreshToken">
/// The raw refresh token the API layer writes into the HttpOnly cookie. Null means "leave the cookie
/// alone": a refresh that lands inside the rotation grace window issues a fresh access token without
/// rotating again, because the cookie the browser already holds is the valid successor.
/// </param>
public sealed record AuthResponse(
    string AccessToken,
    string? RefreshToken,
    DateTimeOffset ExpiresAt);
