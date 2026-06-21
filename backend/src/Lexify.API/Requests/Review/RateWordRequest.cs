namespace Lexify.API.Requests.Review;

public sealed record RateWordRequest(Guid WordId, int Quality);
