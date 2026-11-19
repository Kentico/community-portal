using System.Globalization;
using CMS.ContentEngine;
using Microsoft.AspNetCore.WebUtilities;

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

    /// <summary>
    /// Absolute URL of the request without its query string or trailing slash,
    /// matching the site's routing (<c>AppendTrailingSlash = false</c>).
    /// </summary>
    public static string CanonicalURL(this HttpRequest currentRequest)
    {
        string path = $"{currentRequest.PathBase}{currentRequest.Path}".TrimEnd('/');

        return $"{currentRequest.Scheme}://{currentRequest.Host}{(path.Length == 0 ? "/" : path)}";
    }

    /// <summary>
    /// Appends the request's <c>page</c> query value to a canonical URL when it is 2 or higher,
    /// so each page of a paginated listing is canonical to itself and page 1 to the bare URL.
    /// </summary>
    public static string WithCanonicalPageNumber(this string canonicalURL, HttpRequest currentRequest) =>
        int.TryParse(currentRequest.Query["page"], out int page) && page > 1
            ? QueryHelpers.AddQueryString(canonicalURL, "page", page.ToString(CultureInfo.InvariantCulture))
            : canonicalURL;

    public static string AssetAbsoluteURL(this ContentItemAsset asset, HttpRequest currentRequest) =>
        $"{currentRequest.Scheme}://{currentRequest.Host}{currentRequest.PathBase}{asset.Url.TrimStart('~')}";
}
