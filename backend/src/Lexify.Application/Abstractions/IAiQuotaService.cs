namespace Lexify.Application.Abstractions;

/// <summary>
/// Per-user daily cap on AI calls. This is a *budget*, not a rate limit: AiRateLimiterPolicy already
/// smooths bursts (10/min), but without a daily ceiling a single user could still spend the owner's
/// AI credits all day long. Enforced at the two entry points that reach a provider — word enrichment
/// and test generation.
/// </summary>
public interface IAiQuotaService
{
    Task<AiQuotaCheck> CheckAsync(Guid userId, CancellationToken ct = default);
}

/// <param name="Limit">Configured cap; zero or negative means "no cap".</param>
/// <param name="Used">Calls the user has already made in the current UTC day.</param>
public sealed record AiQuotaCheck(bool IsExceeded, int Limit, int Used)
{
    public static AiQuotaCheck Unlimited { get; } = new(IsExceeded: false, Limit: 0, Used: 0);
}
