using CMS.ContentEngine;
using CMS.DataEngine;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Infrastructure;

public record PortalWebsiteSettingsQuery : IQuery<PortalWebsiteSettingsQueryResponse>;
public record PortalWebsiteSettingsQueryResponse(
    PortalWebsiteSettingsInfo PortalSettings,
    WebsiteCookieBannerSettingsContent CookieBannerSettings,
    WebsiteGlobalContent GlobalContent);

public class PortalWebsiteSettingsQueryHandler(ContentItemQueryTools tools, IInfoProvider<PortalWebsiteSettingsInfo> settingsProvider) : ContentItemQueryHandler<PortalWebsiteSettingsQuery, PortalWebsiteSettingsQueryResponse>(tools)
{
    private readonly IInfoProvider<PortalWebsiteSettingsInfo> settingsProvider = settingsProvider;

    public override async Task<PortalWebsiteSettingsQueryResponse> Handle(PortalWebsiteSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await settingsProvider
            .Get()
            .TopN(1)
            .FirstOrDefaultAsync() ?? throw new Exception($"A valid {PortalWebsiteSettingsInfo.OBJECT_TYPE} record is required by the application.");

        var cookieBannerSettings = await GetCookieBannerSettings(settings);
        var globalContent = await GetGlobalContent(settings);

        return new(settings, cookieBannerSettings, globalContent);
    }

    private async Task<WebsiteCookieBannerSettingsContent> GetCookieBannerSettings(PortalWebsiteSettingsInfo websiteSettings)
    {
        var b = new ContentItemQueryBuilder()
            .ForContentType(
                WebsiteCookieBannerSettingsContent.CONTENT_TYPE_NAME,
                p => p
                    .Where(w => w.WhereContentItem(websiteSettings.PortalWebsiteSettingsWebsiteCookieBannerContent.Select(c => c.Identifier).FirstOrDefault())));
        var contents = await Executor.GetMappedResult<WebsiteCookieBannerSettingsContent>(b, DefaultQueryOptions);

        return contents.First();
    }

    private async Task<WebsiteGlobalContent> GetGlobalContent(PortalWebsiteSettingsInfo websiteSettings)
    {
        var b = new ContentItemQueryBuilder()
            .ForContentType(
                WebsiteGlobalContent.CONTENT_TYPE_NAME,
                p => p
                    .Where(w => w.WhereContentItem(websiteSettings.PortalWebsiteSettingsWebsiteGlobalContent.Select(c => c.Identifier).FirstOrDefault()))
                    .WithLinkedItems(1));
        var contents = await Executor.GetMappedResult<WebsiteGlobalContent>(b, DefaultQueryOptions);

        return contents.First();
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(PortalWebsiteSettingsQuery query, PortalWebsiteSettingsQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder.AllObjects(PortalWebsiteSettingsInfo.OBJECT_TYPE)
            .AllContentItems(WebsiteCookieBannerSettingsContent.CONTENT_TYPE_NAME)
            .Collection(result.GlobalContent.WebsiteGlobalContentLogoImageContent, (c, b) => b.ContentItem(c))
            .AllContentItems(WebsiteGlobalContent.CONTENT_TYPE_NAME);
}

public record WebsiteAlerSettingsQuery(PortalWebsiteSettingsInfo Settings) : IQuery<WebsiteAlertSettingsContent>;
public class WebsiteAlerSettingsQueryHandler(ContentItemQueryTools tools) : ContentItemQueryHandler<WebsiteAlerSettingsQuery, WebsiteAlertSettingsContent>(tools)
{
    public override async Task<WebsiteAlertSettingsContent> Handle(WebsiteAlerSettingsQuery request, CancellationToken cancellationToken)
    {
        var b = new ContentItemQueryBuilder()
            .ForContentType(
                WebsiteAlertSettingsContent.CONTENT_TYPE_NAME,
                p => p
                    .Where(w => w.WhereContentItem(request.Settings.PortalWebsiteSettingsWebsiteAlertSettingsContent.Select(c => c.Identifier).FirstOrDefault()))
                    .WithLinkedItems(1));
        var contents = await Executor.GetMappedResult<WebsiteAlertSettingsContent>(b, DefaultQueryOptions, cancellationToken: cancellationToken);
        return contents.First();
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(WebsiteAlerSettingsQuery query, WebsiteAlertSettingsContent result, ICacheDependencyKeysBuilder builder) =>
        builder
            .Collection(result.WebsiteAlertSettingsContentAlertMessageContents, (c, b) => b.ContentItem(c));
}
