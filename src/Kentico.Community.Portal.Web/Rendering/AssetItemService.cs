using CMS.DataEngine;
using CMS.Helpers;
using CMS.MediaLibrary;
using Kentico.Content.Web.Mvc;

namespace Kentico.Community.Portal.Web.Rendering;

public class AssetItemService(IInfoProvider<MediaFileInfo> mediaFileInfoProvider, IMediaFileUrlRetriever mediaFileUrlRetriever, IHttpContextAccessor contextAccessor, IProgressiveCache cache)
{
    private readonly IInfoProvider<MediaFileInfo> mediaFileInfoProvider = mediaFileInfoProvider;
    private readonly IMediaFileUrlRetriever mediaFileUrlRetriever = mediaFileUrlRetriever;
    private readonly IHttpContextAccessor contextAccessor = contextAccessor;
    private readonly IProgressiveCache cache = cache;

    public async Task<AssetViewModel?> RetrieveMediaFile(AssetRelatedItem? item)
    {
        if (item is null)
        {
            return null;
        }

        var mediaInfo = await mediaFileInfoProvider.GetAsync(item.Identifier);

        if (mediaInfo is null)
        {
            return null;
        }

        var url = mediaFileUrlRetriever.Retrieve(mediaInfo);

        return new(mediaInfo.FileGUID, mediaInfo.FileTitle, url, mediaInfo.FileDescription, mediaInfo.FileExtension);
    }

    /// <summary>
    /// MediaFileURLProvider.GetMediaFileAbsoluteUrl does not work correctly as of v26.3.1 because it
    /// doesn't include the port or the file extension
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public string BuildFullFileUrl(IMediaFileUrl url)
    {
        var req = contextAccessor.HttpContext?.Request;

        if (req is null)
        {
            return "";
        }

        return BuildFullFileUrl(url, req);
    }

    public static string BuildFullFileUrl(IMediaFileUrl url, HttpRequest request)
    {
        var uriBuilder = new UriBuilder(request.Scheme, request.Host.Host, request.Host.Port ?? -1, TrimLeadingTilde(url.RelativePath));
        if (uriBuilder.Uri.IsDefaultPort)
        {
            uriBuilder.Port = -1;
        }

        return uriBuilder.Uri.AbsoluteUri;
    }

    private static string TrimLeadingTilde(string input)
    {
        var span = input.AsSpan();
        if (span.Length > 0 && span[0] == '~')
        {
            span = span[1..];
        }

        return span.ToString();
    }
}
