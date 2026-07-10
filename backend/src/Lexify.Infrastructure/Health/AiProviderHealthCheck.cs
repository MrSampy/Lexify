using Lexify.Infrastructure.Settings;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Lexify.Infrastructure.Health;

/// <summary>
/// Reports degraded/unhealthy when none of the configured AI providers answer /v1/models —
/// surfaces a dead LLM before a user hits "Generate test" and waits on a doomed job.
/// </summary>
public sealed class AiProviderHealthCheck(
    IHttpClientFactory httpClientFactory,
    IOptions<List<AiProviderSettings>> providersOptions)
    : IHealthCheck
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(5);

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // Explicitly-returned results are NOT affected by the failureStatus configured on
        // AddCheck(...) — that parameter only kicks in when the check throws. Since we report
        // failure via a return value (not an exception), we must honor the registered
        // FailureStatus (Degraded) ourselves, or a dead LLM would incorrectly flip /api/health
        // to Unhealthy for the whole app.
        var failureStatus = context.Registration.FailureStatus;

        var providers = providersOptions.Value;
        if (providers.Count == 0)
            return new HealthCheckResult(failureStatus, "No AI providers configured.");

        var failures = new List<string>();

        foreach (var provider in providers)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(ProbeTimeout);
            try
            {
                var http = httpClientFactory.CreateClient($"ai:{provider.Name}");
                using var response = await http.GetAsync("/v1/models", cts.Token);
                if (response.IsSuccessStatusCode)
                    return HealthCheckResult.Healthy($"AI provider '{provider.Name}' is reachable.");

                failures.Add($"{provider.Name}: HTTP {(int)response.StatusCode}");
            }
            catch (Exception ex)
            {
                failures.Add($"{provider.Name}: {ex.GetType().Name}");
            }
        }

        return new HealthCheckResult(
            failureStatus, $"No AI provider reachable ({string.Join("; ", failures)}).");
    }
}
