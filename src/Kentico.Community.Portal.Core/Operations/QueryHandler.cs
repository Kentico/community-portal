using System.Globalization;
using CMS.Helpers;
using CMS.Websites;
using CMS.Websites.Routing;
using MediatR;

namespace Kentico.Community.Portal.Core.Operations;

public class WebPageQueryTools(
    IContentQueryExecutor executor,
    IWebPageQueryResultMapper webPageMapper,
    IContentQueryResultMapper contentMapper,
    IWebPageUrlRetriever urlRetriever,
    IWebsiteChannelContext websiteChannelContext,
    ILinkedItemsDependencyAsyncRetriever dependencyRetriever)
{
    public IContentQueryExecutor Executor { get; } = executor;
    public IWebPageQueryResultMapper WebPageMapper { get; } = webPageMapper;
    public IContentQueryResultMapper ContentMapper { get; } = contentMapper;
    public IWebPageUrlRetriever UrlRetriever { get; } = urlRetriever;
    public IWebsiteChannelContext WebsiteChannelContext { get; } = websiteChannelContext;
    public ILinkedItemsDependencyAsyncRetriever DependencyRetriever { get; } = dependencyRetriever;
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
        DependencyRetriever = tools.DependencyRetriever;
        DefaultQueryOptions = new ContentQueryExecutionOptions { ForPreview = tools.WebsiteChannelContext.IsPreview };
    }

    private readonly HashSet<string> customKeys = new(StringComparer.OrdinalIgnoreCase);

    protected IContentQueryExecutor Executor { get; }
    protected IWebPageQueryResultMapper WebPageMapper { get; }
    protected IContentQueryResultMapper ContentMapper { get; }
    protected IWebPageUrlRetriever UrlRetriever { get; }
    protected ContentQueryExecutionOptions DefaultQueryOptions { get; }
    protected ILinkedItemsDependencyAsyncRetriever DependencyRetriever { get; }


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
        var builder = new CacheDependencyKeysBuilder();
        _ = builder.CustomKeys(customKeys);
        _ = AddDependencyKeys(query, result, builder);

        return [.. builder.GetKeys()];
    }

    public virtual object[] ItemNameParts(TQuery query)
    {
        string channelName = query is IChannelContentQuery channelQuery
            ? channelQuery.ChannelName
            : "";

        return query is ICacheByValueQuery cacheByValueQuery
            ? new object[]
                {
                    query.GetType().Name,
                    channelName,
                    CultureInfo.CurrentCulture.Name,
                    cacheByValueQuery.CacheValueKey
                }
            :
                [
                    query.GetType().Name,
                    channelName,
                    CultureInfo.CurrentCulture.Name,
                ];
    }


    public virtual CacheSettings CustomizeCacheSettings(DefaultQueryCacheSettings settings, IQueryCacheKeysCreator<TQuery, TResult> creator, TQuery query)
    {
        var cs = new CacheSettings(
            cacheMinutes: settings.CacheItemDuration.Minutes,
            useSlidingExpiration: settings.IsSlidingExpiration,
            cacheItemNameParts: creator.ItemNameParts(query));

        return cs;
    }
}

public class ContentItemQueryTools(
    IContentQueryExecutor executor,
    IContentQueryResultMapper contentItemMapper,
    IWebPageQueryResultMapper webPageMapper,
    IWebsiteChannelContext websiteChannelContext,
    ILinkedItemsDependencyAsyncRetriever dependencyRetriever)
{
    public IContentQueryExecutor Executor { get; } = executor;
    public IContentQueryResultMapper ContentItemMapper { get; } = contentItemMapper;
    public IWebPageQueryResultMapper WebPageMapper { get; } = webPageMapper;
    public IWebsiteChannelContext WebsiteChannelContext { get; } = websiteChannelContext;
    public ILinkedItemsDependencyAsyncRetriever DependencyRetriever { get; } = dependencyRetriever;
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
        DependencyRetriever = tools.DependencyRetriever;
        DefaultQueryOptions = new ContentQueryExecutionOptions { ForPreview = tools.WebsiteChannelContext.IsPreview };
    }

    private readonly HashSet<string> customKeys = new(StringComparer.OrdinalIgnoreCase);

    protected IContentQueryExecutor Executor { get; }
    protected IContentQueryResultMapper ContentItemMapper { get; }
    protected IWebPageQueryResultMapper WebPageMapper { get; }
    protected ContentQueryExecutionOptions DefaultQueryOptions { get; }
    protected ILinkedItemsDependencyAsyncRetriever DependencyRetriever { get; }


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
        var builder = new CacheDependencyKeysBuilder();
        _ = builder.CustomKeys(customKeys);
        _ = AddDependencyKeys(query, result, builder);

        return [.. builder.GetKeys()];
    }

    public virtual object[] ItemNameParts(TQuery query)
    {
        string channelName = query is IChannelContentQuery channelQuery
            ? channelQuery.ChannelName
            : "";

        return query is ICacheByValueQuery cacheByValueQuery
            ? new object[]
                {
                    query.GetType().Name,
                    channelName,
                    CultureInfo.CurrentCulture.Name,
                    cacheByValueQuery.CacheValueKey
                }
            :
                [
                    query.GetType().Name,
                    channelName,
                    CultureInfo.CurrentCulture.Name,
                ];
    }

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

}

public abstract class DataItemQueryHandler<TQuery, TResult> :
    IQueryHandler<TQuery, TResult>,
    IQueryCacheKeysCreator<TQuery, TResult>,
    IQueryCacheSettingsCustomizer<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
#pragma warning disable IDE0052
    private readonly DataItemQueryTools tools;
#pragma warning restore IDE0052
    public DataItemQueryHandler(DataItemQueryTools tools) => this.tools = tools;

    private readonly HashSet<string> customKeys = new(StringComparer.OrdinalIgnoreCase);

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
        var builder = new CacheDependencyKeysBuilder();
        _ = builder.CustomKeys(customKeys);
        _ = AddDependencyKeys(query, result, builder);

        return [.. builder.GetKeys()];
    }

    public virtual object[] ItemNameParts(TQuery query)
    {
        string channelName = query is IChannelContentQuery channelQuery
            ? channelQuery.ChannelName
            : "";

        return query is ICacheByValueQuery cacheByValueQuery
            ? new object[]
                {
                    query.GetType().Name,
                    channelName,
                    CultureInfo.CurrentCulture.Name,
                    cacheByValueQuery.CacheValueKey
                }
            :
                [
                    query.GetType().Name,
                    channelName,
                    CultureInfo.CurrentCulture.Name,
                ];
    }

    public virtual CacheSettings CustomizeCacheSettings(DefaultQueryCacheSettings settings, IQueryCacheKeysCreator<TQuery, TResult> creator, TQuery query)
    {
        var cs = new CacheSettings(
            cacheMinutes: settings.CacheItemDuration.Minutes,
            useSlidingExpiration: settings.IsSlidingExpiration,
            cacheItemNameParts: creator.ItemNameParts(query));

        return cs;
    }
}
