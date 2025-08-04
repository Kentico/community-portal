using System.Text.RegularExpressions;
using CMS.ContentEngine.Internal;
using CMS.Core;
using CMS.Helpers;
using CMS.Websites.Routing;
using Newtonsoft.Json;

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Licenses;

/// <summary>
/// Service for reading and processing license files from content hub assets.
/// </summary>
public partial class LicenseFileService(
    IEventLogService eventLogService,
    IWebsiteChannelContext websiteChannelContext,
    IContentItemAssetPathProvider contentItemAssetPathProvider,
    IProgressiveCache cache) : ILicenseFileService
{
    private const int CacheMinutes = 10;

    private readonly IEventLogService eventLogService = eventLogService;
    private readonly IWebsiteChannelContext websiteChannelContext = websiteChannelContext;
    private readonly IContentItemAssetPathProvider contentItemAssetPathProvider = contentItemAssetPathProvider;
    private readonly IProgressiveCache cache = cache;

    // Regex source generator for matching GUIDs in content item asset URLs
    [GeneratedRegex(@"[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}", RegexOptions.IgnoreCase)]
    private static partial Regex ContentItemAssetUrlGuidRegex();

    /// <summary>
    /// Retrieves licenses from a file content asset and formats them with type links.
    /// </summary>
    /// <param name="file">The file content containing license data</param>
    /// <param name="licenseTypeLinks">Dictionary mapping license type names to their URLs</param>
    /// <returns>A view model containing formatted license links</returns>
    public LicensesViewModel GetLicenses(FileContent file, Dictionary<string, string> licenseTypeLinks)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(licenseTypeLinks);

        var asset = file.FileContentAsset;
        string assetUrl = asset.Url;

        // Extract the FIELD_GUID from the asset URL
        // URL pattern: $"{ContentItemAssetConstants.CONTENT_ASSET_URL_PATH_PREFIX}/{CONTENT_ITEM_GUID:guid}/{FIELD_GUID:guid}/{FILE_NAME}"
        var fieldGuid = ExtractFieldGuidFromUrl(assetUrl);

        string locationPath = contentItemAssetPathProvider.GetFileLocation(asset.Metadata, file.SystemFields.ContentItemGUID, fieldGuid);

        int typesHashCode = string.Join("|", licenseTypeLinks.Select(i => $"{i.Key}_{i.Value}")).GetHashCode();
        var licenses = cache.Load(cs =>
        {
            cs.Cached = !websiteChannelContext.IsPreview;

            return GetLicensesInternal(locationPath, licenseTypeLinks);
        }, new CacheSettings(CacheMinutes, $"Licenses_{typesHashCode}_{locationPath.ToLowerInvariant()}"));

        return licenses;
    }

    private LicensesViewModel GetLicensesInternal(string filePath, Dictionary<string, string> licenseTypeLinks)
    {
        var result = new LicensesViewModel()
        {
            Links = []
        };

        try
        {

            string data = CMS.IO.File.ReadAllText(filePath);
            var groups = JsonConvert.DeserializeObject<Dictionary<string, List<LicenseDto>>>(data) ?? [];

            foreach (var linksGroup in groups)
            {
                var group = new LicenseLinkViewModel()
                {
                    Name = linksGroup.Key,
                    Url = GetLicenseTypeLink(linksGroup.Key, licenseTypeLinks) ?? "",
                    Links = linksGroup.Value.Select(item =>
                        new LicenseLinkViewModel() { Name = item.Name, Url = item.Link }).ToList()
                };

                result.Links.Add(group);
            }
        }
        catch (Exception exception)
        {
            eventLogService.LogEvent(EventTypeEnum.Error, nameof(LicenseFileService), nameof(GetLicenses),
                exception.ToString());
        }

        return result;
    }

    private static string? GetLicenseTypeLink(string name, Dictionary<string, string> dictionary)
    {
        if (dictionary == null || dictionary.Count == 0 || string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return dictionary.FirstOrDefault(i => i.Key.Equals(name, StringComparison.InvariantCultureIgnoreCase)).Value;
    }

    private static Guid ExtractFieldGuidFromUrl(string assetUrl)
    {
        try
        {
            // Use the source-generated regex for better performance
            // Pattern: /assets/{CONTENT_ITEM_GUID}/{FIELD_GUID}/{FILE_NAME}
            var matches = ContentItemAssetUrlGuidRegex().Matches(assetUrl);

            // Return the second GUID match (FIELD_GUID)
            if (matches.Count >= 2 && Guid.TryParse(matches[1].Value, out var fieldGuid))
            {
                return fieldGuid;
            }
        }
        catch
        {
            // If parsing fails, return empty GUID
        }

        return Guid.Empty;
    }

    private class LicenseDto
    {
        public string Name { get; set; } = "";
        public string Version { get; set; } = "";
        public string Link { get; set; } = "";
    }
}

public class LicensesViewModel
{
    public List<LicenseLinkViewModel> Links { get; set; } = [];
}

public class LicenseLinkViewModel
{
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public List<LicenseLinkViewModel> Links { get; set; } = [];
}
