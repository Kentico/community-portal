using Kentico.Community.Portal.Web.Rendering;
using MediatR;

namespace Kentico.Community.Portal.Web.Infrastructure;

public class WebPageMetaService
{
    private readonly IMediator mediator;
    private readonly AssetItemService assetItemService;
    private Meta meta = new("", "");

    public WebPageMetaService(
        IMediator mediator,
        AssetItemService assetItemService)
    {
        this.mediator = mediator;
        this.assetItemService = assetItemService;
    }

    public async Task<Meta> GetMeta()
    {
        var resp = await mediator.Send(new WebsiteSettingsContentQuery());
        var settings = resp.Settings;

        string titlePattern = settings.WebsiteSettingsContentPageTitleFormat ?? "{0}";
        string pageTitle = meta.Title;

        string fullTitle = string.Format(titlePattern, pageTitle).Trim(' ').TrimStart('|').Trim(' ');

        meta = meta with { Title = fullTitle };

        if (meta.OGImageURL is null)
        {
            var mediaFile = settings.WebsiteSettingsContentFallbackOGMediaFileImage.FirstOrDefault();
            var asset = await assetItemService.RetrieveMediaFileImage(mediaFile);

            if (asset is not null)
            {
                meta = meta with { OGImageURL = assetItemService.BuildFullFileUrl(asset.URLData) };
            }
        }

        return meta;
    }

    public void SetMeta(Meta meta) => this.meta = meta;
}

public record Meta(string Title, string Description)
{
    public string? CanonicalURL { get; init; }
    public string? OGImageURL { get; init; }
};
