namespace Lexify.API.Requests.Auth;

public sealed record ResendTwoFactorRequest(string ChallengeToken);
