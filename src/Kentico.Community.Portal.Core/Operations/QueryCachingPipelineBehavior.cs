using CMS.Helpers;
using CMS.Websites.Routing;
using MediatR;
using Microsoft.Extensions.Options;

namespace Kentico.Community.Portal.Core.Operations;

/// <summary>
/// Integrates a caching layer into all <see cref="IQueryHandler{TQuery, TResult}"/> using <see cref="IPipelineBehavior{TRequest, TResponse}"/> decoration
/// </summary>
/// <typeparam name="TQuery"></typeparam>
/// <typeparam name="TResult"></typeparam>
public class QueryCachingPipelineBehavior<TQuery, TResult>(
    IProgressiveCache cache,
    IEnumerable<IQueryCacheKeysCreator<TQuery, TResult>> creators,
    IEnumerable<IQueryCacheSettingsCustomizer<TQuery, TResult>> cacheCustomizers,
    IWebsiteChannelContext channelContext,
    IOptions<DefaultQueryCacheSettings> settings) : IPipelineBehavior<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    private readonly IProgressiveCache cache = cache;

    /// <summary>
    /// Could be 0 or 1 items in this collection
    /// </summary>
    private readonly IEnumerable<IQueryCacheKeysCreator<TQuery, TResult>> creators = creators;
    private readonly IEnumerable<IQueryCacheSettingsCustomizer<TQuery, TResult>> cacheCustomizers = cacheCustomizers;
    private readonly IWebsiteChannelContext channelContext = channelContext;
    private readonly DefaultQueryCacheSettings settings = settings.Value;

    public async Task<TResult> Handle(TQuery query, RequestHandlerDelegate<TResult> next, CancellationToken cancellationToken)
    {
        var creator = creators.FirstOrDefault();

        /*
         * Skip caching is we are in preview mode, caching is disabled, or we cannot generate cache keys for the current query
         */
        if (channelContext.IsPreview ||
            !this.settings.IsEnabled ||
            creator is null)
        {
            return await next();
        }

        var customizer = cacheCustomizers.FirstOrDefault();

        var settings = customizer?.CustomizeCacheSettings(this.settings, creator, query) ?? new CacheSettings(
            cacheMinutes: this.settings.CacheItemDuration.Minutes,
            useSlidingExpiration: true,
            cacheItemNameParts: creator.ItemNameParts(query));

        return await cache.LoadAsync((cs, t) => QueryCachingPipelineBehavior<TQuery, TResult>.GetCachedResult(query, creator, next, cs), settings, cancellationToken);
    }

    private static async Task<TResult> GetCachedResult(
        TQuery query,
        IQueryCacheKeysCreator<TQuery, TResult> cacheKeysCreator,
        RequestHandlerDelegate<TResult> next,
        CacheSettings cs)
    {
        var result = await next();

        if (result is Result<TResult> monad && monad.IsFailure)
        {
            cs.Cached = false;
            return result;
        }

        var resultValue = result is Result<TResult> success
            ? success.Value
            : result;

        string[] keys = cacheKeysCreator.DependencyKeys(query, resultValue);

        if (keys.Length == 0)
        {
            cs.Cached = false;
            return result;
        }

        cs.GetCacheDependency = () => CacheHelper.GetCacheDependency(keys);

        return result;
    }
}

