using Kentico.Content.Web.Mvc;
using MediatR;

namespace Kentico.Community.Portal.Web.Infrastructure;

public class WebPageMetaService(
    IMediator mediator,
    IHttpContextAccessor contextAccessor)
{
    private readonly IMediator mediator = mediator;
    private readonly IHttpContextAccessor contextAccessor = contextAccessor;
    private WebPageMeta meta = WebPageMeta.Default;

    public async Task<WebPageMeta> GetMeta()
    {
        var settings = await mediator.Send(new PortalWebsiteSettingsQuery());

        string titlePattern = settings.GlobalContent.WebsiteGlobalContentPageTitleFormat ?? "{0}";
        string pageTitle = meta.Title;

        string fullTitle = string.Format(titlePattern, pageTitle).Trim(' ').TrimStart('|').Trim(' ');

        meta = meta with { Title = fullTitle };

        if (meta.OGImageURL.HasNoValue)
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

    public void SetMeta(WebPageMeta meta) => this.meta = meta;
    public void SetMeta(IWebPageMetaFields metaFields) => meta = new(metaFields);
}
