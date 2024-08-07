﻿using System.Globalization;
using CMS.Helpers;
using CMS.Websites;
using CMS.Websites.Routing;
using MediatR;

namespace Kentico.Community.Portal.Core.Operations;

public class WebPageQueryTools(
    IContentQueryExecutor executor,
    IWebPageUrlRetriever urlRetriever,
    IContentQueryExecutionOptionsCreator queryOptionsCreator,
    ILinkedItemsDependencyAsyncRetriever dependencyRetriever)
{
    public IContentQueryExecutor Executor { get; } = executor;
    public IWebPageUrlRetriever UrlRetriever { get; } = urlRetriever;
    public IContentQueryExecutionOptionsCreator QueryOptionsCreator { get; } = queryOptionsCreator;
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
        UrlRetriever = tools.UrlRetriever;
        DependencyRetriever = tools.DependencyRetriever;
        this.tools = tools;
    }

    private readonly HashSet<string> customKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly WebPageQueryTools tools;

    protected IContentQueryExecutor Executor { get; }
    protected IWebPageUrlRetriever UrlRetriever { get; }
    protected ContentQueryExecutionOptions DefaultQueryOptions => tools.QueryOptionsCreator.Create();
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
    IWebsiteChannelContext websiteChannelContext,
    ILinkedItemsDependencyAsyncRetriever dependencyRetriever,
    IContentQueryExecutionOptionsCreator queryOptionsCreator)
{
    public IContentQueryExecutor Executor { get; } = executor;
    public IWebsiteChannelContext WebsiteChannelContext { get; } = websiteChannelContext;
    public ILinkedItemsDependencyAsyncRetriever DependencyRetriever { get; } = dependencyRetriever;
    public IContentQueryExecutionOptionsCreator QueryOptionsCreator { get; } = queryOptionsCreator;
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
        DependencyRetriever = tools.DependencyRetriever;
        this.tools = tools;
    }

    private readonly HashSet<string> customKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly ContentItemQueryTools tools;

    protected IContentQueryExecutor Executor { get; }
    protected ContentQueryExecutionOptions DefaultQueryOptions => tools.QueryOptionsCreator.Create();
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

public class DataItemQueryTools;

public abstract class DataItemQueryHandler<TQuery, TResult>(DataItemQueryTools tools) :
    IQueryHandler<TQuery, TResult>,
    IQueryCacheKeysCreator<TQuery, TResult>,
    IQueryCacheSettingsCustomizer<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
#pragma warning disable IDE0052
    private readonly DataItemQueryTools tools = tools;

#pragma warning restore IDE0052

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
