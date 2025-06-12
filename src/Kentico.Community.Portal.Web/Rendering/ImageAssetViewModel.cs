using CMS.ContentEngine;

namespace Kentico.Community.Portal.Web.Rendering;

public class ImageAssetViewModel
{
    public Guid ID { get; init; }
    public string Title { get; }
    public string AltText { get; }
    public ContentItemAsset Asset { get; }

    public static ImageAssetViewModel Create(ImageContent content) => new(content);
    public static Maybe<ImageAssetViewModel> Create(IFeaturedImageFields item) =>
        item.FeaturedImageImageContent
            .TryFirst()
            .Map(Create);

    public ImageAssetViewModel(ImageContent content)
    {
        ID = content.SystemFields.ContentItemGUID;
        Title = content.MediaItemTitle;
        AltText = content.MediaItemShortDescription;
        Asset = content.ImageContentAsset;
    }
}

public static class ImageAssetViewModelExtensions
{
    public static Maybe<ImageAssetViewModel> ToImageAssetViewModel(this IFeaturedImageFields item) => ImageAssetViewModel.Create(item);
    public static Maybe<ImageAssetViewModel> ToImageAssetViewModel(this AuthorContent content) =>
        content.AuthorContentPhotoImageContent
            .TryFirst()
            .Map(ImageAssetViewModel.Create);
}
