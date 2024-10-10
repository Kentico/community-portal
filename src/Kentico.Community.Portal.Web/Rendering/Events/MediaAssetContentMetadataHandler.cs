using CMS.ContentEngine;
using CMS.ContentEngine.Internal;
using CMS.Helpers;
using CMS.IO;
using MetadataExtractor;
using MetadataExtractor.Formats.Jpeg;
using MetadataExtractor.Formats.Png;
using MetadataExtractor.Formats.WebP;

namespace Kentico.Community.Portal.Web.Rendering.Events;

public class MediaAssetContentMetadataHandler(
    IHttpContextAccessor accessor,
    IContentItemAssetPathProvider pathProvider)
{
    private readonly IHttpContextAccessor accessor = accessor;
    private readonly IContentItemAssetPathProvider pathProvider = pathProvider;

    public void Handle(CreateContentItemEventArgs args) => HandleInternal(args.ContentTypeName, args.ContentItemData);

    public void Handle(UpdateContentItemDraftEventArgs args) => HandleInternal(args.ContentTypeName, args.ContentItemData);

    private void HandleInternal(string contentTypeName, ContentItemData contentItemData)
    {
        /*
         * Only perform processing when a request is available (eg not during CI restore)
         */
        if (accessor.HttpContext is null)
        {
            return;
        }

        if (!IsSupportedContentType(contentTypeName))
        {
            return;
        }

        if (!GetMetadata(contentItemData).TryGetValue(out var metadata))
        {
            return;
        }

        if (!ImageHelper.IsImage(metadata.Extension))
        {
            return;
        }

        var dimensions = GetDimensions(metadata);

        SetDimensions(contentTypeName, contentItemData, dimensions);
    }

    private static bool IsSupportedContentType(string contentTypeName) =>
        string.Equals(contentTypeName, MediaAssetContent.CONTENT_TYPE_NAME)
        || string.Equals(contentTypeName, ImageContent.CONTENT_TYPE_NAME);

    private static Maybe<ContentItemAssetMetadataWithSource> GetMetadata(ContentItemData contentItemData)
    {
        if (contentItemData.TryGetValue(nameof(MediaAssetContent.MediaAssetContentAssetLight), out ContentItemAssetMetadataWithSource metadata) && metadata is not null)
        {
            return metadata;
        }

        if (contentItemData.TryGetValue(nameof(ImageContent.ImageContentAsset), out metadata) && metadata is not null)
        {
            return metadata;
        }

        return Maybe.None;
    }

    private (int width, int height) GetDimensions(ContentItemAssetMetadataWithSource metadata)
    {
        int width = 0;
        int height = 0;

        string path = pathProvider.GetTempFileLocation(metadata);
        var file = StorageHelper.GetFileInfo(path);

        /*
         * Temporary files might have moved and the metadata might not have been refreshed
         */
        if (file is null || !file.Exists)
        {
            return (width, height);
        }

        using var stream = file.OpenRead();

        var directories = ImageMetadataReader.ReadMetadata(stream);

        foreach (var directory in directories)
        {
            if (directory is JpegDirectory jpegDirectory)
            {
                width = jpegDirectory.GetImageWidth();
                height = jpegDirectory.GetImageHeight();

                return (width, height);
            }
            else if (directory is PngDirectory pngDirectory)
            {
                width = pngDirectory.GetInt32(PngDirectory.TagImageWidth);
                height = pngDirectory.GetInt32(PngDirectory.TagImageHeight);

                return (width, height);
            }
            else if (directory is WebPDirectory webPDirectory)
            {
                width = webPDirectory.GetInt32(WebPDirectory.TagImageWidth);
                height = webPDirectory.GetInt32(WebPDirectory.TagImageHeight);

                return (width, height);
            }
        }

        return (width, height);
    }

    private static void SetDimensions(string contentTypeName, ContentItemData contentItemData, (int width, int height) dimensions)
    {
        var fields = Maybe<(string width, string height)>.None;

        if (string.Equals(contentTypeName, MediaAssetContent.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnoreCase))
        {
            fields = (
                nameof(MediaAssetContent.MediaAssetContentImageLightWidth),
                nameof(MediaAssetContent.MediaAssetContentImageLightHeight)
            );
        }

        if (string.Equals(contentTypeName, ImageContent.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnoreCase))
        {
            fields = (
                nameof(ImageContent.MediaItemAssetWidth),
                nameof(ImageContent.MediaItemAssetHeight)
            );
        }

        fields.Execute(f =>
        {
            contentItemData.SetValue(f.width, dimensions.width);
            contentItemData.SetValue(f.height, dimensions.height);
        });
    }
}
