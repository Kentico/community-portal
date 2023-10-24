using CMS.Websites;

namespace Kentico.Community.Portal.Core.Content;

public static class WebPageExtensions
{
    public static bool IsWebPage(this IWebPageFieldsSource source, IWebPageFieldsSource target) => source.SystemFields.ContentItemID == target.SystemFields.ContentItemID;
}
