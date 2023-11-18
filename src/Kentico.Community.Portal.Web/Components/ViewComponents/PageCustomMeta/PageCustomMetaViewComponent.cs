using Kentico.Community.Portal.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Kentico.Community.Portal.Web.Components.ViewComponents.PageCustomMeta;

public class PageCustomMetaViewComponent : ViewComponent
{
    private readonly IMediator mediator;
    private readonly WebPageMetaService metaService;
    private readonly IHttpContextAccessor contextAccessor;
    private readonly ReCaptchaSettings settings;

    public PageCustomMetaViewComponent(
        IMediator mediator,
        WebPageMetaService metaService,
        IHttpContextAccessor contextAccessor,
        IOptions<ReCaptchaSettings> options)
    {
        this.mediator = mediator;
        this.metaService = metaService;
        this.contextAccessor = contextAccessor;
        settings = options.Value;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var meta = await metaService.GetMeta();

        string url = contextAccessor.HttpContext.Request.GetEncodedUrl();

        var settingsResult = await mediator.Send(new WebsiteSettingsContentQuery());

        var vm = new PageCustomMetaViewModel(meta)
        {
            SiteName = settingsResult.Settings.WebsiteSettingsContentWebsiteDisplayName,
            URL = url,
            // TODO set this based on the current page specifying to include it
            CaptchaSiteKey = settings.SiteKey,
            OGImageURL = meta.OGImageURL
        };

        return View("~/Components/ViewComponents/PageCustomMeta/PageCustomMeta.cshtml", vm);
    }
}

public class PageCustomMetaViewModel
{
    public static PageCustomMetaViewModel Empty { get; } = new PageCustomMetaViewModel();

    public PageCustomMetaViewModel(Meta meta)
    {
        Title = meta.Title;
        Description = meta.Description;
        CanonicalURL = meta.CanonicalURL;
    }

    private PageCustomMetaViewModel()
    {
        OGImageURL = "";
        Title = "";
        Description = "";
        SiteName = "";
        CaptchaSiteKey = null;
        MetaRobotsContent = null;
        CanonicalURL = null;
    }

    public string URL { get; init; } = "";
    public string OGImageURL { get; init; } = "";
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public string? CaptchaSiteKey { get; init; } = null;
    public string SiteName { get; init; } = "";
    public string? MetaRobotsContent { get; init; } = null;
    public string? CanonicalURL { get; init; } = null;
}
