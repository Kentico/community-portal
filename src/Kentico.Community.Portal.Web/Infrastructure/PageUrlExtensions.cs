using CMS.Websites;

namespace Kentico.Content.Web.Mvc;

public static class PageUrlExtensions
{
    /// <summary>
    /// <see cref="PageUrl.RelativePath" /> with the leading '~' character removed
    /// </summary>
    /// <param name="pageUrl"></param>
    /// <returns></returns>
    public static string RelativePathTrimmed(this WebPageUrl pageUrl) => pageUrl.RelativePath.TrimStart('~');
}
