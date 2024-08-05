using System.Text;
using CMS.ContentEngine;
using CMS.ContentEngine.Internal;
using CMS.DataEngine;
using CMS.FormEngine;
using CMS.Helpers;
using CMS.Membership;
using Kentico.Community.Portal.Admin.UIPages;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;

[assembly: UIPage(
    uiPageType: typeof(AssetLinksPage),
    parentType: typeof(ContentItemEditSection),
    name: "Asset Links",
    slug: "asset-links",
    templateName: TemplateNames.OVERVIEW,
    order: 1000)]

namespace Kentico.Community.Portal.Admin.UIPages;

[UIEvaluatePermission(SystemPermissions.VIEW)]
public class AssetLinksPage(IContentQueryExecutor queryExecutor, IContentItemAssetRetriever assetRetriever) : OverviewPage<ContentItemInfo>
{
    private readonly IContentQueryExecutor queryExecutor = queryExecutor;
    private readonly IContentItemAssetRetriever assetRetriever = assetRetriever;

    [PageParameter(typeof(IntPageModelBinder), typeof(ContentItemEditSection))]
    public override int ObjectId { get; set; }

    [PageParameter(typeof(ContentLanguageModelBinder), typeof(ContentHubContentLanguage))]
    public ContentLanguageUrlIdentifier ContentLanguageIdentifier { get; set; } = null!;

    public override async Task<LoadOverviewDataResult> LoadData(CancellationToken cancellationToken)
    {
        var result = await base.LoadData(cancellationToken);
        var info = await GetInfoObject(cancellationToken);

        var dc = DataClassInfoProvider.GetDataClassInfo(info.ContentItemContentTypeID);

        var form = new FormInfo(dc.ClassFormDefinition);

        var fs = form
            .GetFields(true, false)
            .Where(f => string.Equals(f.DataType, "contentitemasset"))
            .ToList();

        if (fs.Count == 0)
        {
            result.Callouts.Add(new CalloutConfiguration
            {
                Headline = "Content item assets",
                Content = "This content item has no content item assets",
                Type = CalloutType.QuickTip
            });

            return result;
        }

        var b = new ContentItemQueryBuilder().ForContentTypes(q => q
            .OfContentType(dc.ClassName)
            .WithContentTypeFields())
            .Parameters(q => q.Where(w => w.WhereEquals(nameof(ContentItemFields.ContentItemID), ObjectId)))
            .InLanguage(ContentLanguageIdentifier.LanguageName);
        var assets = (await queryExecutor.GetResult(
            b,
            resultSelector: MapContainer(fs),
            options: new ContentQueryExecutionOptions { ForPreview = true, IncludeSecuredItems = true },
            cancellationToken: cancellationToken)).FirstOrDefault()!;

        result.Callouts.Add(new CalloutConfiguration
        {
            Headline = "Content item assets",
            Content = BuildAssetHTML(assets),
            ContentAsHtml = true,
            Type = CalloutType.QuickTip
        });

        return result;
    }

    /// <summary>
    /// Dynamically creates <see cref="ContentItemAsset"/> from the query result item data container
    /// </summary>
    /// <param name="assetFields"></param>
    /// <returns></returns>
    private Func<IContentQueryDataContainer, List<(ContentItemAsset, string)>> MapContainer(List<FormFieldInfo> assetFields)
    {
        var fs = assetFields;

        return (container) =>
        {
            var assets = new List<(ContentItemAsset, string)>();

            foreach (var f in fs)
            {
                var asset = assetRetriever.Retrieve(container, f.Name).GetAwaiter().GetResult();
                if (asset is null)
                {
                    continue;
                }

                assets.Add((asset, f.Caption));
            }

            return assets;
        };
    }

    /// <summary>
    /// Creates an HTML string list of asset relative paths and images
    /// </summary>
    /// <param name="assets"></param>
    /// <returns></returns>
    private static string BuildAssetHTML(List<(ContentItemAsset, string)> assets)
    {
        var sb = new StringBuilder()
            .Append("<ul>");
        foreach (var assetItem in assets)
        {
            var (asset, fieldName) = assetItem;

            string url = asset.Url.TrimStart('~');
            _ = sb
                .AppendLine($"""<li style="margin-block-end: 1rem; list-style-type: none;">""")
                    .AppendLine($"""<dl>""")
                        .AppendLine($"""<dt style="font-weight: 600; margin-block-end: 0.5rem">Field</dt>""")
                        .AppendLine($"""<dd>{fieldName}</dd>""")
                        .AppendLine($"""<dt style="font-weight: 600; margin-block-end: 0.5rem">File name</dt>""")
                        .AppendLine($"""<dd>{asset.Metadata.Name}</dd>""")
                        .AppendLine($"""<dt style="font-weight: 600; margin-block-end: 0.5rem">Relative path</dt>""")
                        .AppendLine($"""<dd>{url}</dd>""");

            if (ImageHelper.IsImage(asset.Metadata.Extension))
            {
                _ = sb
                    .AppendLine($"""<dt style="font-weight: 600; margin-block-end: 0.5rem">Image</dt>""")
                    .AppendLine($"""<dd><img src="{url}" style="max-width: 300px;" /></dd>""");
            }

            _ = sb.AppendLine($"""</dl>""")
            .AppendLine($"""</li>""");
        }
        return sb.Append("</ul>").ToString();
    }
}

/// <summary>
/// Adapted from "internal" type
/// </summary>
internal class ContentLanguageModelBinder(string parameterName) : PageModelBinder<ContentLanguageUrlIdentifier>(parameterName)
{
    public override Task<ContentLanguageUrlIdentifier> Bind(PageRouteValues routeValues)
    {
        if (routeValues is null)
        {
            throw new ArgumentNullException(nameof(routeValues));
        }

        if (!routeValues.TryGet(parameterName, out string? value))
        {
            throw new InvalidOperationException("Value for '" + parameterName + "' parameter not found.");
        }

        var contentLanguageInfo = AbstractInfo<ContentLanguageInfo, IInfoProvider<ContentLanguageInfo>>.Provider.Get(value)
            ?? throw new InvalidOperationException("Language with code name '" + value + "' does not exist.");

        return Task.FromResult(new ContentLanguageUrlIdentifier
        {
            ContentLanguageID = contentLanguageInfo.ContentLanguageID,
            LanguageName = value
        });
    }
}
