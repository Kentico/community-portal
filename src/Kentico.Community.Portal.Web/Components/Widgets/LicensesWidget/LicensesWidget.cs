using CMS.MediaLibrary;
using Kentico.Community.Portal.Web.Components.Widgets.Licenses;
using Kentico.Community.Portal.Web.Rendering;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

[assembly: RegisterWidget(
    identifier: LicensesWidgetViewComponent.Identifier,
    viewComponentType: typeof(LicensesWidgetViewComponent),
    name: "3rd party licenses",
    propertiesType: typeof(LicensesWidgetProperties),
    IconClass = "icon-kentico")]

namespace Kentico.Community.Portal.Web.Components.Widgets.Licenses;

public class LicensesWidgetViewComponent(LicensesFacade licensesFacade, AssetItemService itemService) : ViewComponent
{
    public const string Identifier = "CommunityPortal.LicensesWidget";

    private readonly LicensesFacade licensesFacade = licensesFacade;
    private readonly AssetItemService itemService = itemService;

    public async Task<IViewComponentResult> InvokeAsync(LicensesWidgetProperties properties)
    {
        var relatedAsset = properties.LicenseFiles.FirstOrDefault();

        if (relatedAsset is null)
        {
            ModelState.AddModelError("missingLicense", $"No License File has been selected.");

            return View("~/Components/ComponentError.cshtml");
        }

        var asset = await itemService.RetrieveMediaFile(properties.LicenseFiles.FirstOrDefault());

        if (asset is null)
        {
            ModelState.AddModelError("missingLicense", $"Could not find the License File {relatedAsset.Name}: {relatedAsset.Identifier}");

            return View("~/Components/ComponentError.cshtml");
        }

        var model = new LicensesWidgetViewModel()
        {
            Title = properties.Title,
            NoLicensesText = properties.NoLicensesText
        };

        var licenseTypeLinks = GetDictionary(properties.LicensesTypeDescriptionLinks);

        model.Licenses = await licensesFacade.GetLicenses(asset, licenseTypeLinks);

        return View("~/Components/Widgets/LicensesWidget/LicensesWidget.cshtml", model);
    }

    private static Dictionary<string, string> GetDictionary(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        try
        {
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(value) ?? [];
        }
        catch
        {
            return [];
        }
    }
}

public class LicensesWidgetProperties : IWidgetProperties
{
    [TextInputComponent(Label = "Title", Order = 1)]
    public string Title { get; set; } = "";

    [TextInputComponent(Label = "No licenses text", Order = 1)]
    public string NoLicensesText { get; set; } = "";

    [AssetSelectorComponent(
        Label = "Licenses file",
        MaximumAssets = 1,
        AllowedExtensions = "json",
        ExplanationText = "Should be a JSON file containing a dictionary of license identifiers, each with an array of libraries using that license.")]
    public IEnumerable<AssetRelatedItem> LicenseFiles { get; set; } = Enumerable.Empty<AssetRelatedItem>();

    [TextAreaComponent(Label = "Licenses type description links", Order = 4)]
    public string LicensesTypeDescriptionLinks { get; set; } = "";
}

public class LicensesWidgetViewModel
{
    public string Title { get; set; } = "";
    public string NoLicensesText { get; set; } = "";
    public LicensesViewModel Licenses { get; set; } = new();
}
