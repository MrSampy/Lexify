namespace Lexify.Infrastructure.AI;

/// <summary>
/// A process-wide, monotonically increasing counter that seeds the per-call rotation of AI provider
/// keys. Registered as a singleton so the round-robin survives the scoped <see cref="AIOrchestrator"/>
/// instances (a fresh orchestrator per request must not restart the rotation at the first key every time).
/// </summary>
public sealed class AiProviderRotation
{
    private long _counter = -1;

    /// <summary>Returns 0, 1, 2, … on successive calls; thread-safe.</summary>
    public long Next() => Interlocked.Increment(ref _counter);
}
