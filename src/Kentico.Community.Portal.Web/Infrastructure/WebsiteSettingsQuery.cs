using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Infrastructure;

public record WebsiteSettingsContentQuery : IQuery<WebsiteSettingsContent>;
public record WebsiteSettingsContentQueryResponse(WebsiteSettingsContent Settings);

public class WebsiteSettingsContentQueryHandler(ContentItemQueryTools tools) : ContentItemQueryHandler<WebsiteSettingsContentQuery, WebsiteSettingsContent>(tools)
{
    public override async Task<WebsiteSettingsContent> Handle(WebsiteSettingsContentQuery request, CancellationToken cancellationToken)
    {
        var b = new ContentItemQueryBuilder().ForContentType(WebsiteSettingsContent.CONTENT_TYPE_NAME, p => p
            .TopN(1)
            /** MICRO-OPTIMIZATION
             * We don't need web page or content item fields and this explicit list will
             * exclude them from the SELECT
             */
            .Columns([
                nameof(WebsiteSettingsContent.WebsiteSettingsContentAlertBoxContentHTML),
                nameof(WebsiteSettingsContent.WebsiteSettingsContentAlertBoxCookieExpirationDays),
                nameof(WebsiteSettingsContent.WebsiteSettingsContentCookiebannerContentHTML),
                nameof(WebsiteSettingsContent.WebsiteSettingsContentCookieBannerHeading),
                nameof(WebsiteSettingsContent.WebsiteSettingsContentExceptionContentHTML),
                nameof(WebsiteSettingsContent.WebsiteSettingsContentFallbackOGMediaFileImage),
                nameof(WebsiteSettingsContent.WebsiteSettingsContentFormsConfigurationJSON),
                nameof(WebsiteSettingsContent.WebsiteSettingsContentIsAlertBoxEnabled),
                nameof(WebsiteSettingsContent.WebsiteSettingsContentNotFoundContentHTML),
                nameof(WebsiteSettingsContent.WebsiteSettingsContentPageTitleFormat),
                nameof(WebsiteSettingsContent.WebsiteSettingsContentRobotsTxt),
                nameof(WebsiteSettingsContent.WebsiteSettingsContentWebsiteDisplayName),
            ]));

        var r = await Executor.GetMappedResult<WebsiteSettingsContent>(b, DefaultQueryOptions, cancellationToken);

        return r.First();
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(WebsiteSettingsContentQuery query, WebsiteSettingsContent result, ICacheDependencyKeysBuilder builder) =>
        builder.AllContentItems(WebsiteSettingsContent.CONTENT_TYPE_NAME);
}
