using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Internal;
using Kentico.Community.Portal.Admin.UIPages;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Websites.UIPages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

[assembly: PageExtender(typeof(PreviewTabExtender))]

namespace Kentico.Community.Portal.Admin.UIPages;

/// <summary>
/// Adds "Open published page" action button to the web page preview tab UI
/// </summary>
/// <param name="urlRetriever"></param>
/// <param name="infoProvider"></param>
/// <param name="contextAccessor"></param>
/// <param name="queryExecutor"></param>
public class PreviewTabExtender(IWebPageUrlRetriever urlRetriever, IWebsiteChannelDomainProvider domainProvider, IHttpContextAccessor contextAccessor, IContentQueryExecutor queryExecutor)
    : PageExtender<PreviewTab>
{
    private readonly IWebPageUrlRetriever urlRetriever = urlRetriever;
    private readonly IWebsiteChannelDomainProvider domainProvider = domainProvider;
    private readonly IHttpContextAccessor contextAccessor = contextAccessor;
    private readonly IContentQueryExecutor queryExecutor = queryExecutor;

    public override async Task<TemplateClientProperties> ConfigureTemplateProperties(TemplateClientProperties properties)
    {
        var props = await base.ConfigureTemplateProperties(properties);

        if (props is not PreviewPageClientProperties clientProps ||
            clientProps.WebPageState.MenuActions is not IReadOnlyCollection<IContentItemAction> readOnlyActions ||
            // This could break in the future if the underlying collection changes
            // We should avoid this type of downcasting, but the init only read-only collection doesn't give us many options
            readOnlyActions is not List<ContentItemAction> mutableActions)
        {
            return props;
        }

        var b = new ContentItemQueryBuilder()
            .ForContentTypes(q => q.ForWebsite([Page.WebPageIdentifier.WebPageItemID]))
            .InLanguage(Page.WebPageIdentifier.LanguageName);
        var items = await queryExecutor.GetMappedResult<IWebPageFieldsSource>(b, new ContentQueryExecutionOptions { ForPreview = false, IncludeSecuredItems = true });

        // Item is unpublished, so we can't generate a valid "published" URL for it
        if (!items.Any())
        {
            return props;
        }

        var webPageUrl = await urlRetriever.Retrieve(Page.WebPageIdentifier.WebPageItemID, Page.WebPageIdentifier.LanguageName);

        string relativeUrl = webPageUrl.RelativePath;
        relativeUrl = relativeUrl.StartsWith("~/")
            ? relativeUrl[1..]
            : relativeUrl;

        string domain = await domainProvider.GetDomain(Page.ApplicationIdentifier.WebsiteChannelID);
        string fullUrl = UriHelper.BuildAbsolute(
            scheme: contextAccessor.HttpContext!.Request.Scheme,
            host: new HostString(domain),
            pathBase: "",
            path: relativeUrl);

        mutableActions.Add(new ContentItemAction
        {
            Name = "OpenPublishedPage",
            Label = "↗️ Open Published Page",
            Tooltip = "Opens the current page in a new tab",
            Permission = WebPageAclPermissions.DISPLAY,
            Url = fullUrl,
            Icon = Icons.ArrowRightTopSquare,
            ButtonColor = ButtonColor.Primary,
        });

        return props;
    }
}

