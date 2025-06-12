using CMS.ContentEngine;
using CMS.ContentEngine.Internal;
using CMS.Core;
using CMS.DataEngine;
using CMS.FormEngine;
using CMS.Websites.Internal;
using Kentico.Community.Portal.Admin;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Kentico.Community.Portal.Web.Rendering;

public class MarkdownContentItemReferenceExtractor(
    MarkdownRenderer markdownRenderer,
    IInfoProvider<WebPageUrlPathInfo> urlProvider,
    IConversionService conversionService,
    IInfoProvider<WebsiteChannelInfo> websiteChannelProvider,
    IWebsiteChannelDomainProvider domainProvider) : IFormFieldContentItemReferenceExtractor
{
    private readonly MarkdownRenderer markdownRenderer = markdownRenderer;
    private readonly IInfoProvider<WebPageUrlPathInfo> urlProvider = urlProvider;
    private readonly IConversionService conversionService = conversionService;
    private readonly IInfoProvider<WebsiteChannelInfo> websiteChannelProvider = websiteChannelProvider;
    private readonly IWebsiteChannelDomainProvider domainProvider = domainProvider;

    public bool CanExtractReferences(FormFieldInfo fieldInfo)
    {
        if (fieldInfo is null || !string.Equals(fieldInfo.DataType, "longtext", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (fieldInfo.Settings is null || fieldInfo.Settings["controlname"] is not string controlName)
        {
            return false;
        }

        return string.Equals(controlName, MarkdownFormComponent.IDENTIFIER, StringComparison.OrdinalIgnoreCase);
    }

    public IEnumerable<ContentItemReference> Extract(object fieldValue)
    {
        if (fieldValue is not string markdown)
        {
            return [];
        }

        var channelIDs = websiteChannelProvider.Get()
            .Column(nameof(WebsiteChannelInfo.WebsiteChannelID))
            .GetEnumerableTypedResultAsync()
            .GetAwaiter()
            .GetResult()
            .Select(c => c.WebsiteChannelID);

        List<Uri> allDomains = [];

        foreach (int id in channelIDs)
        {
            var domains = domainProvider.GetAllDomains(id, default)
                .GetAwaiter()
                .GetResult();

            allDomains.AddRange(domains);
        }

        var document = markdownRenderer.Parse(markdown);

        var linkPaths = document
            .Descendants()
            .Where(d => d is LinkInline link)
            .Cast<LinkInline>()
            .Select(l => l.Url ?? "")
            .Select(url =>
            {
                // Remove query string and anchor
                string cleanUrl = url.Split('?', '#')[0];

                // Try to parse as absolute URI
                if (Uri.TryCreate(cleanUrl, UriKind.Absolute, out var absUri))
                {
                    // Check if domain matches any in allDomains
                    if (allDomains.Any(domain => Uri.Compare(domain, absUri, UriComponents.Host, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0))
                    {
                        // Use the path part, remove leading slash
                        return absUri.AbsolutePath.TrimStart('/');
                    }
                }
                else if (Uri.TryCreate(cleanUrl, UriKind.Relative, out var relUri))
                {
                    // For relative URIs, just trim leading slash
                    return cleanUrl.TrimStart('/');
                }

                // If not a valid URI, skip
                return null;
            })
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToList();

        if (linkPaths.Count == 0)
        {
            return [];
        }

        /**
         * Most projects should also filter this query on the channel ID to handle identical web page paths across different website channels
         * But the Kentico Community Portal only has 1 website channel so we're simplifying this process.
         */
        var results = urlProvider.Get()
            .Source(s => s.Join<WebPageItemInfo>(nameof(WebPageUrlPathInfo.WebPageUrlPathWebPageItemID), nameof(WebPageItemInfo.WebPageItemID)))
            .Source(s => s.Join<ContentItemInfo>(nameof(WebPageItemInfo.WebPageItemContentItemID), nameof(ContentItemInfo.ContentItemID)))
            .WhereIn(nameof(WebPageUrlPathInfo.WebPageUrlPath), linkPaths)
            .Columns(nameof(ContentItemInfo.ContentItemGUID))
            .Distinct()
            .GetDataContainerResult()
            .ToList();

        if (results.Count == 0)
        {
            return [];
        }

        var identifiers = results
            .Select(r => conversionService.GetGuid(r.GetValue(nameof(ContentItemInfo.ContentItemGUID)), default))
            .Where(id => id != default)
            .Select(id => new ContentItemReference { Identifier = id })
            .ToList();

        return identifiers;
    }
}
