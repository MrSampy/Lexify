namespace Lexify.API.Requests.Auth;

public sealed record VerifyTwoFactorRequest(string ChallengeToken, string Code);
