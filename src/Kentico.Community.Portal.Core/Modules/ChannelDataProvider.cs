using CMS.DataEngine;
using CMS.Helpers;
using CMS.Websites;
using CMS.Websites.Internal;

namespace Kentico.Community.Portal.Core;

public interface IChannelDataProvider
{
    public Task<string?> GetChannelNameByWebsiteChannelID(int websiteChannelID, CancellationToken cancellationToken = default);
    public Task<string?> GetWebsiteChannelDomainByChannelCodeName(string channelCodeName, CancellationToken cancellationToken = default);
}

public class ChannelDataProvider(
    IInfoProvider<ChannelInfo> channelProvider,
    IWebsiteChannelDomainProvider domainProvider,
    IProgressiveCache cache,
    ICacheDependencyBuilderFactory cacheFactory)
    : IChannelDataProvider
{
    private readonly IInfoProvider<ChannelInfo> channelProvider = channelProvider;
    private readonly IWebsiteChannelDomainProvider domainProvider = domainProvider;
    private readonly IProgressiveCache cache = cache;
    private readonly ICacheDependencyBuilderFactory cacheFactory = cacheFactory;

    public Task<string?> GetChannelNameByWebsiteChannelID(int websiteChannelID, CancellationToken cancellationToken = default)
    {
        var builder = cacheFactory.Create();

        return cache.LoadAsync(cs => channelProvider.Get()
            .Source(s => s.Join<WebsiteChannelInfo>(nameof(ChannelInfo.ChannelID), nameof(WebsiteChannelInfo.WebsiteChannelChannelID)))
            .WhereEquals(nameof(WebsiteChannelInfo.WebsiteChannelID), websiteChannelID)
            .Columns(nameof(ChannelInfo.ChannelName))
            .GetScalarResultAsync<string?>(cancellationToken: cancellationToken), new(30, [nameof(ChannelDataProvider), nameof(GetChannelNameByWebsiteChannelID)])
            {
                CacheDependency = builder.ForInfoObjects<ChannelInfo>().All().Builder().Build()
            });
    }

    public async Task<string?> GetWebsiteChannelDomainByChannelCodeName(string channelCodeName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(channelCodeName))
        {
            return null;
        }

        var builder = cacheFactory.Create();

        return await cache.LoadAsync(async cs =>
        {
            cs.CacheDependency = builder.ForInfoObjects<ChannelInfo>().All().Builder().Build();

            int? websiteChannelID = await channelProvider.Get()
                .Source(s => s.Join<WebsiteChannelInfo>(nameof(ChannelInfo.ChannelID), nameof(WebsiteChannelInfo.WebsiteChannelChannelID)))
                .WhereEquals(nameof(ChannelInfo.ChannelName), channelCodeName)
                .Columns(nameof(WebsiteChannelInfo.WebsiteChannelID))
                .GetScalarResultAsync<int?>(cancellationToken: cancellationToken);

            if (!websiteChannelID.HasValue)
            {
                return null;
            }

            return await domainProvider.GetDomain(websiteChannelID.Value, cancellationToken);
        }, new(30, [nameof(ChannelDataProvider), nameof(GetWebsiteChannelDomainByChannelCodeName), channelCodeName]));
    }
}
