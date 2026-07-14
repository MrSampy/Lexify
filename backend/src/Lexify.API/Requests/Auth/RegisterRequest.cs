namespace Lexify.API.Requests.Auth;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string? DisplayName,
    string? InviteCode = null);
