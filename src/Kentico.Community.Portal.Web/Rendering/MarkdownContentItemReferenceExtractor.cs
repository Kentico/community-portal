using CMS.ContentEngine;
using CMS.ContentEngine.Internal;
using CMS.Core;
using CMS.DataEngine;
using CMS.Websites.Internal;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Kentico.Community.Portal.Web.Rendering;

public class MarkdownContentItemReferenceExtractor(
    MarkdownRenderer markdownRenderer,
    IInfoProvider<WebPageUrlPathInfo> urlProvider,
    IConversionService conversionService) : IContentItemReferenceExtractor
{
    private readonly MarkdownRenderer markdownRenderer = markdownRenderer;
    private readonly IInfoProvider<WebPageUrlPathInfo> urlProvider = urlProvider;
    private readonly IConversionService conversionService = conversionService;

    public IEnumerable<ContentItemReference> Extract(object fieldValue)
    {
        if (fieldValue is not string markdown)
        {
            return [];
        }

        var document = markdownRenderer.Parse(markdown);

        var relativePaths = document
            .Descendants()
            .Where(d => d is LinkInline link)
            .Cast<LinkInline>()
            .Select(l => l.Url ?? "")
            .Where(url => url.StartsWith('/'))
            .Select(p => p.TrimStart('/'));

        var results = urlProvider.Get()
            .Source(s => s.Join<WebPageItemInfo>(nameof(WebPageUrlPathInfo.WebPageUrlPathWebPageItemID), nameof(WebPageItemInfo.WebPageItemID)))
            .Source(s => s.Join<ContentItemInfo>(nameof(WebPageItemInfo.WebPageItemContentItemID), nameof(ContentItemInfo.ContentItemID)))
            .WhereIn(nameof(WebPageUrlPathInfo.WebPageUrlPath), relativePaths)
            .Columns(nameof(ContentItemInfo.ContentItemGUID))
            .GetDataContainerResult();

        var identifiers = results
            .Select(r => conversionService.GetGuid(r.GetValue(nameof(ContentItemInfo.ContentItemGUID)), default))
            .Where(id => id != default)
            .Select(id => new ContentItemReference { Identifier = id })
            .ToList();

        return identifiers;
    }
}
