namespace Kentico.Community.Portal.Core.Operations;

/// <summary>
/// Configuration values used for <see cref="IQueryCacheSettingsCustomizer{TQuery,TResponse}"/> caching
/// </summary>
public class DefaultQueryCacheSettings
{
    public bool IsEnabled { get; set; } = true;
    public bool IsSlidingExpiration { get; set; } = true;
    public TimeSpan CacheItemDuration { get; set; } = TimeSpan.FromMinutes(5);
}
