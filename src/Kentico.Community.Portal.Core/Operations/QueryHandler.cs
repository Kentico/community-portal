using System.Globalization;
using CMS.Helpers;
using CMS.Websites;
using CMS.Websites.Routing;
using MediatR;

namespace Kentico.Community.Portal.Core.Operations;

public class WebPageQueryTools
{
    public IContentQueryExecutor Executor { get; }
    public IWebPageQueryResultMapper WebPageMapper { get; }
    public IContentQueryResultMapper ContentMapper { get; }
    public IWebPageUrlRetriever UrlRetriever { get; }
    public IWebsiteChannelContext WebsiteChannelContext { get; }

    public WebPageQueryTools(
        IContentQueryExecutor executor,
        IWebPageQueryResultMapper webPageMapper,
        IContentQueryResultMapper contentMapper,
        IWebPageUrlRetriever urlRetriever,
        IWebsiteChannelContext websiteChannelContext)
    {
        Executor = executor;
        WebPageMapper = webPageMapper;
        ContentMapper = contentMapper;
        UrlRetriever = urlRetriever;
        WebsiteChannelContext = websiteChannelContext;
    }
}

public interface IQueryHandler<in TQuery, TResult> : IRequestHandler<TQuery, TResult> where TQuery : IQuery<TResult> { }

public abstract class WebPageQueryHandler<TQuery, TResult> :
    IQueryHandler<TQuery, TResult>,
    IQueryCacheKeysCreator<TQuery, TResult>,
    IQueryCacheSettingsCustomizer<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    public WebPageQueryHandler(WebPageQueryTools tools)
    {
        Executor = tools.Executor;
        WebPageMapper = tools.WebPageMapper;
        ContentMapper = tools.ContentMapper;
        UrlRetriever = tools.UrlRetriever;
        WebsiteChannelContext = tools.WebsiteChannelContext;
        DefaultQueryOptions = new ContentQueryExecutionOptions { ForPreview = tools.WebsiteChannelContext.IsPreview, IncludeSecuredItems = false };
    }

    private readonly HashSet<string> customKeys = new(StringComparer.OrdinalIgnoreCase);

    protected IContentQueryExecutor Executor { get; }
    protected IWebPageQueryResultMapper WebPageMapper { get; }
    protected IContentQueryResultMapper ContentMapper { get; }
    protected IWebPageUrlRetriever UrlRetriever { get; }
    protected IWebsiteChannelContext WebsiteChannelContext { get; }
    protected ContentQueryExecutionOptions DefaultQueryOptions { get; }


    public abstract Task<TResult> Handle(TQuery request, CancellationToken cancellationToken);

    /// <summary>
    /// Overridable to explicitly set the cache dependency keys based on the <typeparamref name="TQuery"/> and <typeparamref name="TResult"/>
    /// </summary>
    /// <param name="query"></param>
    /// <param name="result"></param>
    /// <param name="builder"></param>
    /// <returns></returns>
    protected virtual ICacheDependencyKeysBuilder AddDependencyKeys(TQuery query, TResult result, ICacheDependencyKeysBuilder builder) =>
        query switch
        {
            WebPageRoutedQuery<TResult> q => builder.WebPage(q.Page.WebPageItemID, q.Page.LanguageName),
            WebPageByGUIDQuery<TResult> q => builder.WebPage(q.WebPageGUID),
            WebPageByIDQuery<TResult> q => builder.WebPage(q.WebPageID),
            _ => builder
        };

    public string[] DependencyKeys(TQuery query, TResult result)
    {
        var builder = new CacheDependencyKeysBuilder(WebsiteChannelContext);
        _ = builder.CustomKeys(customKeys);
        _ = AddDependencyKeys(query, result, builder);

        return builder.GetKeys().ToArray();
    }

    public virtual object[] ItemNameParts(TQuery query) =>
        query is ICacheByValueQuery cacheByValueQuery
            ? new object[]
                {
                    query.GetType().Name,
                    WebsiteChannelContext.WebsiteChannelName,
                    CultureInfo.CurrentCulture.Name,
                    cacheByValueQuery.CacheValueKey
                }
            : new object[]
                {
                    query.GetType().Name,
                    WebsiteChannelContext.WebsiteChannelName,
                    CultureInfo.CurrentCulture.Name,
                };

    public virtual CacheSettings CustomizeCacheSettings(DefaultQueryCacheSettings settings, IQueryCacheKeysCreator<TQuery, TResult> creator, TQuery query)
    {
        var cs = new CacheSettings(
            cacheMinutes: settings.CacheItemDuration.Minutes,
            useSlidingExpiration: settings.IsSlidingExpiration,
            cacheItemNameParts: creator.ItemNameParts(query));

        return cs;
    }
}

public class ContentItemQueryTools
{
    public IContentQueryExecutor Executor { get; }
    public IContentQueryResultMapper ContentItemMapper { get; }
    public IWebPageQueryResultMapper WebPageMapper { get; }
    public IWebsiteChannelContext WebsiteChannelContext { get; }

    public ContentItemQueryTools(IContentQueryExecutor executor, IContentQueryResultMapper contentItemMapper, IWebPageQueryResultMapper webPageMapper, IWebsiteChannelContext websiteChannelContext)
    {
        Executor = executor;
        ContentItemMapper = contentItemMapper;
        WebPageMapper = webPageMapper;
        WebsiteChannelContext = websiteChannelContext;
    }
}

public abstract class ContentItemQueryHandler<TQuery, TResult> :
    IQueryHandler<TQuery, TResult>,
    IQueryCacheKeysCreator<TQuery, TResult>,
    IQueryCacheSettingsCustomizer<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    public ContentItemQueryHandler(ContentItemQueryTools tools)
    {
        Executor = tools.Executor;
        ContentItemMapper = tools.ContentItemMapper;
        WebPageMapper = tools.WebPageMapper;
        WebsiteChannelContextContext = tools.WebsiteChannelContext;
        DefaultQueryOptions = new ContentQueryExecutionOptions { ForPreview = tools.WebsiteChannelContext.IsPreview };
    }

    private readonly HashSet<string> customKeys = new(StringComparer.OrdinalIgnoreCase);

    protected IContentQueryExecutor Executor { get; }
    protected IContentQueryResultMapper ContentItemMapper { get; }
    protected IWebPageQueryResultMapper WebPageMapper { get; }
    protected IWebsiteChannelContext WebsiteChannelContextContext { get; }
    protected ContentQueryExecutionOptions DefaultQueryOptions { get; }


    public abstract Task<TResult> Handle(TQuery request, CancellationToken cancellationToken);

    /// <summary>
    /// Overridable to explicitly set the cache dependency keys based on the <typeparamref name="TQuery"/> and <typeparamref name="TResult"/>
    /// </summary>
    /// <param name="query"></param>
    /// <param name="result"></param>
    /// <param name="builder"></param>
    /// <returns></returns>
    protected abstract ICacheDependencyKeysBuilder AddDependencyKeys(TQuery query, TResult result, ICacheDependencyKeysBuilder builder);

    public string[] DependencyKeys(TQuery query, TResult result)
    {
        var builder = new CacheDependencyKeysBuilder(WebsiteChannelContextContext);
        _ = builder.CustomKeys(customKeys);
        _ = AddDependencyKeys(query, result, builder);

        return builder.GetKeys().ToArray();
    }

    public virtual object[] ItemNameParts(TQuery query) =>
        query is ICacheByValueQuery cacheByValueQuery
            ? new object[]
                {
                        query.GetType().Name,
                        WebsiteChannelContextContext.WebsiteChannelName,
                        CultureInfo.CurrentCulture.Name,
                        cacheByValueQuery.CacheValueKey
                }
            : new object[]
                {
                        query.GetType().Name,
                        WebsiteChannelContextContext.WebsiteChannelName,
                        CultureInfo.CurrentCulture.Name,
                };
    public virtual CacheSettings CustomizeCacheSettings(DefaultQueryCacheSettings settings, IQueryCacheKeysCreator<TQuery, TResult> creator, TQuery query)
    {
        var cs = new CacheSettings(
            cacheMinutes: settings.CacheItemDuration.Minutes,
            useSlidingExpiration: settings.IsSlidingExpiration,
            cacheItemNameParts: creator.ItemNameParts(query));

        return cs;
    }
}

public class DataItemQueryTools
{
    public IWebsiteChannelContext WebsiteChannelContext { get; }

    public DataItemQueryTools(IWebsiteChannelContext websiteChannelContext) => WebsiteChannelContext = websiteChannelContext;
}

public abstract class DataItemQueryHandler<TQuery, TResult> :
    IQueryHandler<TQuery, TResult>,
    IQueryCacheKeysCreator<TQuery, TResult>,
    IQueryCacheSettingsCustomizer<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    public DataItemQueryHandler(DataItemQueryTools tools) => WebsiteChannelContextContext = tools.WebsiteChannelContext;

    private readonly HashSet<string> customKeys = new(StringComparer.OrdinalIgnoreCase);

    protected IWebsiteChannelContext WebsiteChannelContextContext { get; }


    public abstract Task<TResult> Handle(TQuery request, CancellationToken cancellationToken);

    /// <summary>
    /// Overridable to explicitly set the cache dependency keys based on the <typeparamref name="TQuery"/> and <typeparamref name="TResult"/>
    /// </summary>
    /// <param name="query"></param>
    /// <param name="result"></param>
    /// <param name="builder"></param>
    /// <returns></returns>
    protected abstract ICacheDependencyKeysBuilder AddDependencyKeys(TQuery query, TResult result, ICacheDependencyKeysBuilder builder);

    public string[] DependencyKeys(TQuery query, TResult result)
    {
        var builder = new CacheDependencyKeysBuilder(WebsiteChannelContextContext);
        _ = builder.CustomKeys(customKeys);
        _ = AddDependencyKeys(query, result, builder);

        return builder.GetKeys().ToArray();
    }

    public virtual object[] ItemNameParts(TQuery query) =>
        query is ICacheByValueQuery cacheByValueQuery
            ? new object[]
                {
                        query.GetType().Name,
                        WebsiteChannelContextContext.WebsiteChannelName,
                        CultureInfo.CurrentCulture.Name,
                        cacheByValueQuery.CacheValueKey
                }
            : new object[]
                {
                        query.GetType().Name,
                        WebsiteChannelContextContext.WebsiteChannelName,
                        CultureInfo.CurrentCulture.Name,
                };
    public virtual CacheSettings CustomizeCacheSettings(DefaultQueryCacheSettings settings, IQueryCacheKeysCreator<TQuery, TResult> creator, TQuery query)
    {
        var cs = new CacheSettings(
            cacheMinutes: settings.CacheItemDuration.Minutes,
            useSlidingExpiration: settings.IsSlidingExpiration,
            cacheItemNameParts: creator.ItemNameParts(query));

        return cs;
    }
}
