namespace Kentico.Content.Web.Mvc;

public static class PageUrlExtensions
{
    /// <summary>
    /// <see cref="PageUrl.RelativePath" /> with the leading '~' character removed
    /// </summary>
    /// <param name="pageUrl"></param>
    /// <returns></returns>
    public static string RelativePathTrimmed(this WebPageUrl pageUrl) => pageUrl.RelativePath.TrimStart('~');

    public static string AbsoluteURL(this WebPageUrl pageUrl, HttpRequest currentRequest) =>
        $"{currentRequest.Scheme}://{currentRequest.Host}{currentRequest.PathBase}{pageUrl.RelativePathTrimmed()}";
}
