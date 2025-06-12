namespace Kentico.Community.Portal.Web.Rendering;

public class ImageViewModel
{
    public Guid ID { get; init; }
    public string Title { get; }
    public string AltText { get; }
    public int Width { get; }
    public int Height { get; }
    public string URL { get; }

    public static ImageViewModel Create(ImageContent content) => new(content);
    public static Maybe<ImageViewModel> Create(IFeaturedImageFields item) =>
        item.FeaturedImageImageContent
            .TryFirst()
            .Map(Create);

    public ImageViewModel(string title, string altText, int width, int height, string url)
    {
        Title = title;
        AltText = altText;
        Width = width;
        Height = height;
        URL = url;
    }

    public ImageViewModel(ImageContent content)
    {
        ID = content.SystemFields.ContentItemGUID;
        Title = content.MediaItemTitle;
        AltText = content.MediaItemShortDescription;
        Width = content.ImageContentAsset.Metadata.Width ?? 0;
        Height = content.ImageContentAsset.Metadata.Height ?? 0;
        URL = content.ImageContentAsset.Url;
    }
}

public static class ImageViewModelExtensions
{
    public static Maybe<ImageViewModel> ToImageViewModel(this IFeaturedImageFields item) => ImageViewModel.Create(item);
    public static Maybe<ImageViewModel> ToImageViewModel(this AuthorContent content) =>
        content.AuthorContentPhotoImageContent
            .TryFirst()
            .Map(ImageViewModel.Create);
}
