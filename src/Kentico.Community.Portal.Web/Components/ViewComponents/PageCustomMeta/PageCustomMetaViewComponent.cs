using CMS.Websites.Routing;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using MediatR;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Kentico.Community.Portal.Web.Components.ViewComponents.PageCustomMeta;

public class PageCustomMetaViewComponent(
    IMediator mediator,
    WebPageMetaService metaService,
    IHttpContextAccessor contextAccessor,
    IOptions<ReCaptchaSettings> recpatchaOptions,
    IContentRetriever contentRetriever,
    IWebPageUrlRetriever urlRetriever,
    IWebPageDataContextRetriever dataContextRetriever,
    IWebsiteChannelContext websiteChannelContext
    ) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var meta = await metaService.GetMeta();

        string url = contextAccessor.HttpContext?.Request.GetEncodedUrl() ?? "";

        var settings = await mediator.Send(new PortalWebsiteSettingsQuery());

        var vm = new PageCustomMetaViewModel(meta)
        {
            SiteName = settings.GlobalContent.WebsiteGlobalContentDisplayName,
            URL = url,
            CaptchaSiteKey = Maybe.From(recpatchaOptions.Value.SiteKey).MapNullOrWhiteSpaceAsNone(),
            OGImageURL = meta.OGImageURL,
            TwitterCard = meta.OGImageURL.HasValue && !meta.OGImageIsFallback ? "summary_large_image" : "summary",
            Feeds = await GetFeeds(),
            MetaRobotsContent = meta.Robots.MapNullOrWhiteSpaceAsNone(),
            CanonicalURL = await SetCanonicalURL(meta),
            GoogleSiteVerification = Maybe.From(settings.GlobalContent.WebsiteGlobalContentGoogleSiteVerification).MapNullOrWhiteSpaceAsNone(),
            BingSiteVerification = Maybe.From(settings.GlobalContent.WebsiteGlobalContentBingSiteVerification).MapNullOrWhiteSpaceAsNone()
        };

        return View("~/Components/ViewComponents/PageCustomMeta/PageCustomMeta.cshtml", vm);
    }

    private async Task<Maybe<string>> SetCanonicalURL(WebPageMeta meta)
    {
        if (contextAccessor.HttpContext is not HttpContext context)
        {
            return meta.CanonicalURL;
        }

        if (meta.CanonicalURL.TryGetValue(out string? canonicalURL))
        {
            return IsSiteRelative(canonicalURL)
                ? canonicalURL.RelativePathToAbsoluteURL(context.Request)
                : canonicalURL;
        }

        // Error responses are not indexable and are re-executed on /error/{code}
        if (context.Response.StatusCode >= StatusCodes.Status400BadRequest)
        {
            return Maybe.None;
        }

        if (dataContextRetriever.TryRetrieve(out var _)
            && await contentRetriever.RetrieveCurrentPage<IWebPageFieldsSource>() is { } page)
        {
            var url = await urlRetriever.Retrieve(page);
            if (!string.IsNullOrWhiteSpace(url.AbsoluteUrl))
            {
                return url.AbsoluteUrl.WithCanonicalPageNumber(context.Request);
            }
        }

        return context.Request.CanonicalURL();
    }

    /// <summary>
    /// RSS feed pages (blog, Q&amp;A) for autodiscovery, resolved from the content tree.
    /// None outside a website channel (e.g. error pages on the admin service domain).
    /// </summary>
    private async Task<IReadOnlyList<FeedLink>> GetFeeds()
    {
        if (contextAccessor.HttpContext is not HttpContext context
            || websiteChannelContext.WebsiteChannelID <= 0)
        {
            return [];
        }

        string[] feedContentTypes = [BlogPostPage.CONTENT_TYPE_NAME, QAndAQuestionPage.CONTENT_TYPE_NAME];
        var feedPages = await contentRetriever.RetrievePages<RSSFeedPage>();
        var feeds = new List<FeedLink>();
        foreach (var page in feedPages
            .Where(p => feedContentTypes.Contains(p.RSSFeedPageWebPageContentType, StringComparer.OrdinalIgnoreCase))
            .OrderBy(p => Array.FindIndex(feedContentTypes, t => string.Equals(t, p.RSSFeedPageWebPageContentType, StringComparison.OrdinalIgnoreCase))))
        {
            var url = await urlRetriever.Retrieve(page);
            feeds.Add(new(page.RSSFeedPageFeedName, url.WebPageAbsoluteURL(context.Request)));
        }

        return feeds;
    }

    private static bool IsSiteRelative(string url) =>
        url.StartsWith("~/", StringComparison.Ordinal)
        || (url.StartsWith('/') && !url.StartsWith("//", StringComparison.Ordinal));
}

public class PageCustomMetaViewModel
{
    public PageCustomMetaViewModel(WebPageMeta meta)
    {
        Title = meta.Title;
        Description = meta.Description;
        CanonicalURL = meta.CanonicalURL;
        OGType = meta.OGType;
        Article = meta.Article;
    }

    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public string URL { get; init; } = "";
    public Maybe<string> OGImageURL { get; init; }
    public string OGType { get; init; } = WebPageMeta.OG_TYPE_WEBSITE;
    public Maybe<WebPageArticleMeta> Article { get; init; }
    /// <summary>
    /// <c>summary_large_image</c> when the page has its own image, otherwise <c>summary</c>
    /// </summary>
    public string TwitterCard { get; init; } = "summary";
    public IReadOnlyList<FeedLink> Feeds { get; init; } = [];
    public string SiteName { get; init; } = "";
    public Maybe<string> MetaRobotsContent { get; init; }
    public Maybe<string> CanonicalURL { get; init; }
    public Maybe<string> CaptchaSiteKey { get; init; }
    public Maybe<string> GoogleSiteVerification { get; init; }
    public Maybe<string> BingSiteVerification { get; init; }
}

/// <param name="Title">Feed name</param>
/// <param name="URL">Absolute feed URL</param>
public record FeedLink(string Title, string URL);
