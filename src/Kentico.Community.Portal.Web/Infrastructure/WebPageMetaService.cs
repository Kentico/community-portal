using Kentico.Content.Web.Mvc;
using MediatR;

namespace Kentico.Community.Portal.Web.Infrastructure;

public class WebPageMetaService(
    IMediator mediator,
    IHttpContextAccessor contextAccessor)
{
    private readonly IMediator mediator = mediator;
    private readonly IHttpContextAccessor contextAccessor = contextAccessor;
    private WebpageMeta meta = new("", "");

    public async Task<WebpageMeta> GetMeta()
    {
        var settings = await mediator.Send(new PortalWebsiteSettingsQuery());

        string titlePattern = settings.GlobalContent.WebsiteGlobalContentPageTitleFormat ?? "{0}";
        string pageTitle = meta.Title;

        string fullTitle = string.Format(titlePattern, pageTitle).Trim(' ').TrimStart('|').Trim(' ');

        meta = meta with { Title = fullTitle };

        if (meta.OGImageURL is null)
        {
            var imageContent = settings.GlobalContent.WebsiteGlobalContentLogoImageContent.FirstOrDefault();

            if (imageContent is not null && contextAccessor.HttpContext is HttpContext context)
            {
                string absoluteAssetURL = imageContent.ImageContentAsset.AssetAbsoluteURL(context.Request);
                meta = meta with { OGImageURL = absoluteAssetURL };
            }
        }

        return meta;
    }

    public void SetMeta(WebpageMeta meta) => this.meta = meta;
}
