namespace Kentico.Community.Portal.Web.Rendering;

public class ImageViewModel
{
    public Guid ID { get; init; }
    public string Title { get; }
    public string AltText { get; }
    public int Width { get; }
    public int Height { get; }
    public string URL { get; }

    public static ImageViewModel Create(MediaAssetContent content) => new(content);
    public static ImageViewModel Create(ImageContent content) => new(content);
    public static Maybe<ImageViewModel> Create(IListableItem item) =>
        item.ListableItemFeaturedImageContent
            .TryFirst()
            .Map(Create)
            .IfNoValue(item.ListableItemFeaturedImage
                .TryFirst()
                .Map(Create));

    public ImageViewModel(string title, string altText, int width, int height, string url)
    {
        Title = title;
        AltText = altText;
        Width = width;
        Height = height;
        URL = url;
    }

    public ImageViewModel(MediaAssetContent content)
    {
        ID = content.SystemFields.ContentItemGUID;
        Title = content.MediaAssetContentTitle;
        AltText = content.MediaAssetContentShortDescription;
        Width = content.MediaAssetContentImageLightWidth;
        Height = content.MediaAssetContentImageLightHeight;
        URL = content.MediaAssetContentAssetLight.Url;
    }

    public ImageViewModel(ImageContent content)
    {
        ID = content.SystemFields.ContentItemGUID;
        Title = content.MediaItemTitle;
        AltText = content.MediaItemShortDescription;
        Width = content.MediaItemAssetWidth;
        Height = content.MediaItemAssetHeight;
        URL = content.ImageContentAsset.Url;
    }
}

public static class ImageViewModelExtensions
{
    public static Maybe<ImageViewModel> ToImageViewModel(this IListableItem item) => ImageViewModel.Create(item);
    public static Maybe<ImageViewModel> ToImageViewModel(this AuthorContent content) =>
        content.AuthorContentPhotoImageContent
            .TryFirst()
            .Map(ImageViewModel.Create);
}
