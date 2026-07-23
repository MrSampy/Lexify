using Lexify.Infrastructure.Settings;

namespace Lexify.Infrastructure.AI;

/// <summary>
/// Decides the order in which <see cref="AIOrchestrator"/> tries the configured providers for a single
/// operation. Interchangeable keys — entries sharing a <see cref="AiProviderSettings.BaseUrl"/>, e.g. the
/// several Ollama Cloud keys behind one endpoint — are round-robined so load spreads across them and no
/// single key hits its rate limit first. Distinct endpoints keep their configured order, so a genuine
/// fallback provider (a different <c>BaseUrl</c>) still comes after its primary.
/// </summary>
public static class AiProviderOrdering
{
    /// <param name="seed">
    /// A rotating value (see <see cref="AiProviderRotation"/>); increment it once per operation so the
    /// starting key advances by one each call.
    /// </param>
    public static IReadOnlyList<AiProviderSettings> Order(
        IReadOnlyList<AiProviderSettings> providers, long seed)
    {
        if (providers.Count <= 1) return providers;

        var ordered = new List<AiProviderSettings>(providers.Count);

        // GroupBy preserves first-seen key order and source order within each group, so distinct
        // endpoints keep their configured fallback order and only same-endpoint keys get rotated.
        foreach (var group in providers.GroupBy(p => p.BaseUrl))
        {
            var members = group.ToList();
            if (members.Count == 1)
            {
                ordered.Add(members[0]);
                continue;
            }

            // Non-negative modulo: seed can wrap past long.MaxValue into negatives over a very long uptime.
            var offset = (int)(((seed % members.Count) + members.Count) % members.Count);
            for (var k = 0; k < members.Count; k++)
                ordered.Add(members[(offset + k) % members.Count]);
        }

        return ordered;
    }
}
