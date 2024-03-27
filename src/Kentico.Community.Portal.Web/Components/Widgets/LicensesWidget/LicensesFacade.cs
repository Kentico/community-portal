using System.Net.Http.Headers;
using CMS.Core;
using CMS.Helpers;
using Kentico.Community.Portal.Web.Rendering;
using Newtonsoft.Json;

namespace Kentico.Community.Portal.Web.Components.Widgets.Licenses;

public class LicensesFacade(
    IHttpClientFactory httpClientFactory,
    IEventLogService eventLogService,
    AssetItemService itemService,
    IProgressiveCache cache)
{
    private const string UserAgent = "Kentico-Agent";
    private const string UserAgentVersion = "1.0";

    private readonly IHttpClientFactory httpClientFactory = httpClientFactory;
    private readonly IEventLogService eventLogService = eventLogService;
    private readonly AssetItemService itemService = itemService;
    private readonly IProgressiveCache cache = cache;

    public async Task<LicensesViewModel> GetLicenses(AssetViewModel asset, Dictionary<string, string> licenseTypeLinks)
    {
        string url = itemService.BuildFullFileUrl(asset.URLData);

        int typesHashCode = string.Join("|", licenseTypeLinks.Select(i => $"{i.Key}_{i.Value}")).GetHashCode();
        var licenses = await cache.LoadAsync(async cs =>
        {
            return await GetLicensesInternal(url, licenseTypeLinks);
        }, new CacheSettings(10, $"Licenses_{typesHashCode}_{url.ToLowerInvariant()}"));

        return licenses;
    }

    private async Task<LicensesViewModel> GetLicensesInternal(string url, Dictionary<string, string> licenseTypeLinks)
    {
        var httpClient = httpClientFactory.CreateClient();

        var result = new LicensesViewModel()
        {
            Links = []
        };

        try
        {
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(UserAgent, UserAgentVersion));

            string data = await httpClient.GetStringAsync(url);
            var groups = JsonConvert.DeserializeObject<Dictionary<string, List<LicenseDto>>>(data) ?? [];

            foreach (var linksGroup in groups)
            {
                var group = new LicenseLinkViewModel()
                {
                    Name = linksGroup.Key,
                    Url = GetLicenseTypeLink(linksGroup.Key, licenseTypeLinks) ?? "",
                    Links = linksGroup.Value.Select((item, index) =>
                        new LicenseLinkViewModel() { Name = item.Name, Url = item.Link }).ToList()
                };

                result.Links.Add(@group);
            }
        }
        catch (Exception exception)
        {
            eventLogService.LogEvent(EventTypeEnum.Error, nameof(LicensesFacade), nameof(GetLicenses),
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
