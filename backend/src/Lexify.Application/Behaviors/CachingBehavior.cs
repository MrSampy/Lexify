using Lexify.Application.Abstractions;
using MediatR;

namespace Lexify.Application.Behaviors;

public sealed class CachingBehavior<TRequest, TResponse>(
    ICacheService cache)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICacheable cacheable)
            return await next(cancellationToken);

        var cached = await cache.GetAsync<TResponse>(cacheable.CacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var response = await next(cancellationToken);
        await cache.SetAsync(cacheable.CacheKey, response, cacheable.CacheDuration, cancellationToken);
        return response;
    }
}
