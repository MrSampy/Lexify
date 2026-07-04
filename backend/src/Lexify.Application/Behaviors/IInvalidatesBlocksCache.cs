namespace Lexify.Application.Behaviors;

/// <summary>
/// Marker for commands whose success makes the current user's cached block lists
/// (keys "blocks:{userId}:*") stale. <see cref="BlocksCacheInvalidationBehavior{TRequest,TResponse}"/>
/// clears them after the handler (and the pipeline's SaveChanges) completes.
/// </summary>
public interface IInvalidatesBlocksCache;
