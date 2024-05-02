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

    public void Handle(UpdateContentItemDraftEventArgs args)
    {
        /*
         * Only perform processing when a request is available (eg not during CI restore)
         */
        if (accessor.HttpContext is null)
        {
            return;
        }

        if (!string.Equals(args.ContentTypeName, MediaAssetContent.CONTENT_TYPE_NAME))
        {
            return;
        }

        if (!args.ContentItemData.TryGetValue(nameof(MediaAssetContent.MediaAssetContentAssetLight), out ContentItemAssetMetadataWithSource metadata) || metadata is null)
        {
            return;
        }

        if (!ImageHelper.IsImage(metadata.Extension))
        {
            return;
        }

        var (width, height) = GetDimensions(metadata);

        args.ContentItemData.SetValue(nameof(MediaAssetContent.MediaAssetContentImageLightWidth), width);
        args.ContentItemData.SetValue(nameof(MediaAssetContent.MediaAssetContentImageLightHeight), height);
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
}
