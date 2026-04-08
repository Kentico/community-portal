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
    IWebPageDataContextRetriever dataContextRetriever
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
            MetaRobotsContent = meta.Robots.MapNullOrWhiteSpaceAsNone(),
            CanonicalURL = await SetCanonicalURL(meta)
        };

        return View("~/Components/ViewComponents/PageCustomMeta/PageCustomMeta.cshtml", vm);
    }

    private async Task<Maybe<string>> SetCanonicalURL(WebPageMeta meta)
    {
        if (meta.CanonicalURL.HasValue)
        {
            return meta.CanonicalURL;
        }

        if (!dataContextRetriever.TryRetrieve(out var _))
        {
            return Maybe.None;
        }

        var page = await contentRetriever.RetrieveCurrentPage<IWebPageFieldsSource>();
        if (page is null)
        {
            return Maybe.None;
        }

        var url = await urlRetriever.Retrieve(page);
        return url.AbsoluteUrl;
    }
}

public class PageCustomMetaViewModel
{
    public PageCustomMetaViewModel(WebPageMeta meta)
    {
        Title = meta.Title;
        Description = meta.Description;
        CanonicalURL = meta.CanonicalURL;
    }

    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public string URL { get; init; } = "";
    public Maybe<string> OGImageURL { get; init; }
    public string SiteName { get; init; } = "";
    public Maybe<string> MetaRobotsContent { get; init; }
    public Maybe<string> CanonicalURL { get; init; }
    public Maybe<string> CaptchaSiteKey { get; init; }
}
