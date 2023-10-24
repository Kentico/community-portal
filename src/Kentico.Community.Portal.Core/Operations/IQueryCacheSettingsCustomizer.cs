using CMS.Helpers;

namespace Kentico.Community.Portal.Core.Operations;

/// <summary>
/// Implementing type can customize cache settings for the <see cref="IQuery{TResponse}" />
/// </summary>
/// <typeparam name="TQuery"></typeparam>
/// <typeparam name="TResponse"></typeparam>
public interface IQueryCacheSettingsCustomizer<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    /// <summary>
    /// Creates <see cref="CacheSettings" /> based on the <see cref="CacheSettings.CacheItemNameParts" /> returned by <paramref name="creator" />
    /// the global <paramref name="settings" /> and, if needed, the <paramref name="query" />
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="creator"></param>
    /// <param name="query"></param>
    /// <returns></returns>
    CacheSettings CustomizeCacheSettings(DefaultQueryCacheSettings settings, IQueryCacheKeysCreator<TQuery, TResponse> creator, TQuery query);
}
