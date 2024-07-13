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
    private readonly ReCaptchaSettings settings = options.Value;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var meta = await metaService.GetMeta();

        string url = contextAccessor.HttpContext?.Request.GetEncodedUrl() ?? "";

        var settings = await mediator.Send(new WebsiteSettingsContentQuery());

        var vm = new PageCustomMetaViewModel(meta)
        {
            SiteName = settings.WebsiteSettingsContentWebsiteDisplayName,
            URL = url,
            // TODO set this based on the current page specifying to include it
            CaptchaSiteKey = this.settings.SiteKey,
            OGImageURL = meta.OGImageURL,
            MetaRobotsContent = meta.Robots
        };

        return View("~/Components/ViewComponents/PageCustomMeta/PageCustomMeta.cshtml", vm);
    }
}

public class PageCustomMetaViewModel
{
    public static PageCustomMetaViewModel Empty { get; } = new PageCustomMetaViewModel();

    public PageCustomMetaViewModel(WebpageMeta meta)
    {
        Title = meta.Title;
        Description = meta.Description;
        CanonicalURL = string.IsNullOrWhiteSpace(meta.CanonicalURL)
            ? Maybe<string>.None
            : meta.CanonicalURL;
    }

    private PageCustomMetaViewModel()
    {
        OGImageURL = "";
        Title = "";
        Description = "";
        SiteName = "";
        CaptchaSiteKey = null;
        MetaRobotsContent = null;
    }

    public string URL { get; init; } = "";
    public string? OGImageURL { get; init; } = "";
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public string? CaptchaSiteKey { get; init; } = null;
    public string SiteName { get; init; } = "";
    public string? MetaRobotsContent { get; init; } = null;
    public Maybe<string> CanonicalURL { get; init; }
}
