namespace Kentico.Community.Portal.Core.Content;

public class PortalPage
{
    public string Title { get; }
    public string ShortDescription { get; }

    public PortalPage(IBasicItemFields basicItem)
    {
        Title = basicItem.BasicItemTitle ?? string.Empty;
        ShortDescription = basicItem.BasicItemShortDescription ?? string.Empty;
    }

    // public PortalPage(IWebPageMetaFields webPageMeta)
    // {
    //     Title = webPageMeta.WebPageMetaTitle ?? string.Empty;
    //     ShortDescription = webPageMeta.WebPageMetaShortDescription ?? string.Empty;
    // }

    public PortalPage(string title, string shortDescription)
    {
        Title = title ?? string.Empty;
        ShortDescription = shortDescription ?? string.Empty;
    }
}
