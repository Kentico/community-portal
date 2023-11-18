using CMS.Websites;
using CMS.Websites.Routing;
using Kentico.Content.Web.Mvc;

namespace CMS.ContentEngine;

public static class ContentItemQueryBuilderExtensions
{
    public static ContentItemQueryBuilder ForWebPage(this ContentItemQueryBuilder builder, IWebsiteChannelContext channelContext, RoutedWebPage page) =>
        builder
            .ForContentType(page.ContentTypeName, queryParameters =>
            {
                _ = queryParameters
                    .Where(w => w.WhereEquals(nameof(WebPageFields.WebPageItemID), page.WebPageItemID))
                    .ForWebsite(channelContext.WebsiteChannelName)
                    .TopN(1);
            })
            .InLanguage(page.LanguageName);
    public static ContentItemQueryBuilder ForWebPage(this ContentItemQueryBuilder builder, IWebsiteChannelContext channelContext, RoutedWebPage page, Action<ContentTypeQueryParameters> configureParameters) =>
        builder
            .ForContentType(page.ContentTypeName, queryParameters =>
            {
                _ = queryParameters
                    .Where(w => w.WhereEquals(nameof(WebPageFields.WebPageItemID), page.WebPageItemID))
                    .ForWebsite(channelContext.WebsiteChannelName)
                    .TopN(1);

                configureParameters(queryParameters);
            })
            .InLanguage(page.LanguageName);
    public static ContentItemQueryBuilder ForWebPage(this ContentItemQueryBuilder builder, string channelName, RoutedWebPage page, Action<ContentTypeQueryParameters> configureParameters) =>
        builder
            .ForContentType(page.ContentTypeName, queryParameters =>
            {
                _ = queryParameters
                    .Where(w => w.WhereEquals(nameof(WebPageFields.WebPageItemID), page.WebPageItemID))
                    .ForWebsite(channelName)
                    .TopN(1);

                configureParameters(queryParameters);
            })
            .InLanguage(page.LanguageName);
    public static ContentItemQueryBuilder ForWebPage(this ContentItemQueryBuilder builder, string channelName, RoutedWebPage page) =>
        builder
            .ForContentType(page.ContentTypeName, queryParameters =>
            {
                _ = queryParameters
                    .Where(w => w.WhereEquals(nameof(WebPageFields.WebPageItemID), page.WebPageItemID))
                    .ForWebsite(channelName)
                    .TopN(1);
            })
            .InLanguage(page.LanguageName);
    public static ContentItemQueryBuilder ForWebPage(this ContentItemQueryBuilder builder, IWebsiteChannelContext channelContext, string contentTypeName, int webPageID) =>
        builder
            .ForContentType(contentTypeName, queryParameters =>
            {
                _ = queryParameters
                    .Where(w => w.WhereEquals(nameof(WebPageFields.WebPageItemID), webPageID))
                    .ForWebsite(channelContext.WebsiteChannelName);
            });
    public static ContentItemQueryBuilder ForWebPage(this ContentItemQueryBuilder builder, IWebsiteChannelContext channelContext, string contentTypeName, int webPageID, Action<ContentTypeQueryParameters> configureParameters) =>
        builder
            .ForContentType(contentTypeName, queryParameters =>
            {
                _ = queryParameters
                    .Where(w => w.WhereEquals(nameof(WebPageFields.WebPageItemID), webPageID))
                    .ForWebsite(channelContext.WebsiteChannelName);

                configureParameters(queryParameters);
            });
    public static ContentItemQueryBuilder ForWebPage(this ContentItemQueryBuilder builder, IWebsiteChannelContext channelContext, string contentTypeName, Guid webPageGUID) =>
        builder
            .ForContentType(contentTypeName, queryParameters =>
            {
                _ = queryParameters
                    .Where(w => w.WhereEquals(nameof(WebPageFields.WebPageItemGUID), webPageGUID))
                    .ForWebsite(channelContext.WebsiteChannelName);
            });
    public static ContentItemQueryBuilder ForWebPage(this ContentItemQueryBuilder builder, IWebsiteChannelContext channelContext, string contentTypeName, Guid webPageGUID, Action<ContentTypeQueryParameters> configureParameters) =>
        builder
            .ForContentType(contentTypeName, queryParameters =>
            {
                _ = queryParameters
                    .Where(w => w.WhereEquals(nameof(WebPageFields.WebPageItemGUID), webPageGUID))
                    .ForWebsite(channelContext.WebsiteChannelName);

                configureParameters(queryParameters);
            });
}
