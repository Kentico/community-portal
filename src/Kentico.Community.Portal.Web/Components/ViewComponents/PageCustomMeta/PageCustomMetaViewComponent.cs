using Kentico.Community.Portal.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Kentico.Community.Portal.Web.Components.ViewComponents.PageCustomMeta;

public class PageCustomMetaViewComponent(
    IMediator mediator,
    WebPageMetaService metaService,
    IHttpContextAccessor contextAccessor,
    IOptions<ReCaptchaSettings> options) : ViewComponent
{
    private readonly IMediator mediator = mediator;
    private readonly WebPageMetaService metaService = metaService;
    private readonly IHttpContextAccessor contextAccessor = contextAccessor;
    private readonly ReCaptchaSettings reCaptchaSettings = options.Value;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var meta = await metaService.GetMeta();

        string url = contextAccessor.HttpContext?.Request.GetEncodedUrl() ?? "";

        var settings = await mediator.Send(new PortalWebsiteSettingsQuery());

        var vm = new PageCustomMetaViewModel(meta)
        {
            SiteName = settings.GlobalContent.WebsiteGlobalContentDisplayName,
            URL = url,
            CaptchaSiteKey = Maybe.From(reCaptchaSettings.SiteKey).MapNullOrWhiteSpaceAsNone(),
            OGImageURL = Maybe.From(meta.OGImageURL).MapNullOrWhiteSpaceAsNone(),
            MetaRobotsContent = Maybe.From(meta.Robots).MapNullOrWhiteSpaceAsNone()
        };

        return View("~/Components/ViewComponents/PageCustomMeta/PageCustomMeta.cshtml", vm);
    }
}

public class PageCustomMetaViewModel
{
    public PageCustomMetaViewModel(WebpageMeta meta)
    {
        Title = meta.Title;
        Description = meta.Description;
        CanonicalURL = string.IsNullOrWhiteSpace(meta.CanonicalURL)
            ? Maybe<string>.None
            : meta.CanonicalURL;
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
