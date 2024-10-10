using CMS.Websites;
using CMS.Websites.Routing;
using Kentico.Content.Web.Mvc;

namespace CMS.ContentEngine;

public static class ContentItemQueryBuilderExtensions
{
    public static ContentItemQueryBuilder ForWebPage(this ContentItemQueryBuilder builder, RoutedWebPage page) =>
        builder
            .ForContentType(page.ContentTypeName, queryParameters =>
            {
                _ = queryParameters
                    .Where(w => w.WhereEquals(nameof(WebPageFields.WebPageItemID), page.WebPageItemID))
                    .ForWebsite(page.WebsiteChannelName);
            })
            .InLanguage(page.LanguageName);
    public static ContentItemQueryBuilder ForWebPage(this ContentItemQueryBuilder builder, RoutedWebPage page, Action<ContentTypeQueryParameters> configureParameters) =>
        builder
            .ForContentType(page.ContentTypeName, queryParameters =>
            {
                _ = queryParameters
                    .Where(w => w.WhereEquals(nameof(WebPageFields.WebPageItemID), page.WebPageItemID))
                    .ForWebsite(page.WebsiteChannelName);

                configureParameters(queryParameters);
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
    public static ContentItemQueryBuilder ForWebPage(this ContentItemQueryBuilder builder, string contentTypeName, Guid webPageGUID) =>
       builder
           .ForContentTypes(p => p
                .OfContentType(contentTypeName)
                .WithContentTypeFields()
                .ForWebsite())
            .Parameters(p => p
                .Where(w => w.WhereEquals(nameof(IWebPageFieldsSource.SystemFields.WebPageItemGUID), webPageGUID)));
    public static ContentItemQueryBuilder ForWebPage(this ContentItemQueryBuilder builder, string contentTypeName, Guid webPageGUID, Action<ContentTypesQueryParameters> configureParameters) =>
       builder
           .ForContentTypes(p =>
                configureParameters(p
                    .OfContentType(contentTypeName)
                    .WithContentTypeFields()
                    .ForWebsite()))
            .Parameters(p => p
                .Where(w => w.WhereEquals(nameof(IWebPageFieldsSource.SystemFields.WebPageItemGUID), webPageGUID)));
    public static ContentItemQueryBuilder ForWebPage(this ContentItemQueryBuilder builder, string contentTypeName, Guid webPageGUID, Action<ContentTypesQueryParameters> configureTypesParameters, Action<ContentQueryParameters> configureParameters) =>
        builder
            .ForContentTypes(p =>
                configureTypesParameters(p
                    .OfContentType(contentTypeName)
                    .WithContentTypeFields()
                    .ForWebsite()))
            .Parameters(p =>
                configureParameters(p
                    .Where(w => w.WhereEquals(nameof(IWebPageFieldsSource.SystemFields.WebPageItemGUID), webPageGUID))));

    public static ContentTypeQueryParameters ForContentItem(this ContentTypeQueryParameters p, Guid contentItemGUID) =>
        p.Where(w => w.WhereEquals(nameof(ContentItemFields.ContentItemGUID), contentItemGUID));

    public static ContentTypeQueryParameters ForContentItem(this ContentTypeQueryParameters p, int contentItemID) =>
        p.Where(w => w.WhereEquals(nameof(ContentItemFields.ContentItemID), contentItemID));
}
