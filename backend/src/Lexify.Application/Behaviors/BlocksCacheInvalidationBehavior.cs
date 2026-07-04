using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Behaviors;

/// <summary>
/// Clears the current user's cached GetBlocks pages after a successful command marked with
/// <see cref="IInvalidatesBlocksCache"/>. Registered outside TransactionBehavior so the cache
/// is dropped only after changes are actually saved.
/// </summary>
public sealed class BlocksCacheInvalidationBehavior<TRequest, TResponse>(
    ICacheService cache,
    ICurrentUserService currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);

        if (request is IInvalidatesBlocksCache
            && response is not IResult { IsSuccess: false }
            && currentUser.IsAuthenticated)
        {
            await cache.RemoveByPrefixAsync($"blocks:{currentUser.UserId}:", cancellationToken);
        }

        return response;
    }
}
