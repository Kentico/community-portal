using CMS.ContentEngine;

namespace Kentico.Content.Web.Mvc;

public static class URLExtensions
{
    /// <summary>
    /// <see cref="PageUrl.RelativePath" /> with the leading '~' character removed
    /// </summary>
    /// <param name="pageUrl"></param>
    /// <returns></returns>
    public static string RelativePathTrimmed(this WebPageUrl pageUrl) => pageUrl.RelativePath.TrimStart('~');
    /// <summary>
    /// <see cref="ContentItemAsset.Url" /> with the leading '~' character removed
    /// </summary>
    /// <param name="asset"></param>
    /// <returns></returns>
    public static string RelativePathTrimmed(this ContentItemAsset asset) => asset.Url.TrimStart('~');
    /// <summary>
    /// URL string with the leading '~' character removed
    /// </summary>
    /// <param name="asset"></param>
    /// <returns></returns>
    public static string RelativePathTrimmed(this string url) => url.TrimStart('~');

    public static string WebPageAbsoluteURL(this WebPageUrl pageUrl, HttpRequest currentRequest) =>
        $"{currentRequest.Scheme}://{currentRequest.Host}{currentRequest.PathBase}{pageUrl.RelativePathTrimmed()}";

    public static string RelativePathToAbsoluteURL(this string relativeUrl, HttpRequest currentRequest) =>
        $"{currentRequest.Scheme}://{currentRequest.Host}{currentRequest.PathBase}{relativeUrl.TrimStart('~')}";

    public static string AssetAbsoluteURL(this ContentItemAsset asset, HttpRequest currentRequest) =>
        $"{currentRequest.Scheme}://{currentRequest.Host}{currentRequest.PathBase}{asset.Url.TrimStart('~')}";
}
