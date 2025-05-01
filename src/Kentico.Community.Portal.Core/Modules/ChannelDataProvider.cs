using CMS.DataEngine;
using CMS.Helpers;
using CMS.Websites;

namespace Kentico.Community.Portal.Core;

public interface IChannelDataProvider
{
    public Task<string?> GetChannelNameByWebsiteChannelID(int websiteChannelID);
}

public class ChannelDataProvider(
    IInfoProvider<ChannelInfo> channelProvider,
    IProgressiveCache cache,
    ICacheDependencyBuilderFactory cacheFactory)
    : IChannelDataProvider
{
    private readonly IInfoProvider<ChannelInfo> channelProvider = channelProvider;
    private readonly IProgressiveCache cache = cache;
    private readonly ICacheDependencyBuilderFactory cacheFactory = cacheFactory;

    public Task<string?> GetChannelNameByWebsiteChannelID(int websiteChannelID)
    {
        var builder = cacheFactory.Create();

        return cache.LoadAsync(cs => channelProvider.Get()
            .Source(s => s.Join<WebsiteChannelInfo>(nameof(ChannelInfo.ChannelID), nameof(WebsiteChannelInfo.WebsiteChannelChannelID)))
            .WhereEquals(nameof(WebsiteChannelInfo.WebsiteChannelID), websiteChannelID)
            .Columns(nameof(ChannelInfo.ChannelName))
            .GetScalarResultAsync<string?>(), new(30, [nameof(ChannelDataProvider), nameof(GetChannelNameByWebsiteChannelID)])
            {
                CacheDependency = builder.ForInfoObjects<ChannelInfo>().All().Builder().Build()
            });
    }
}
