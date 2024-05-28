using Kentico.Community.Portal.Web.Rendering;
using MediatR;

namespace Kentico.Community.Portal.Web.Infrastructure;

public class WebPageMetaService(
    IMediator mediator,
    AssetItemService assetItemService)
{
    private readonly IMediator mediator = mediator;
    private readonly AssetItemService assetItemService = assetItemService;
    private Meta meta = new("", "");

    public async Task<Meta> GetMeta()
    {
        var settings = await mediator.Send(new WebsiteSettingsContentQuery());

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
    public Meta(IWebPageMeta meta) : this(meta.WebPageMetaTitle, meta.WebPageMetaDescription) => Robots = meta.WebPageMetaRobots;

    public string? CanonicalURL { get; init; }
    public string? OGImageURL { get; init; }
    public string? Robots { get; set; } = null;
};
