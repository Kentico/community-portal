using CMS.MediaLibrary;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Licenses;
using Kentico.Community.Portal.Web.Rendering;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

[assembly: RegisterWidget(
    identifier: LicensesWidget.IDENTIFIER,
    viewComponentType: typeof(LicensesWidget),
    name: "3rd party licenses",
    propertiesType: typeof(LicensesWidgetProperties),
    IconClass = KenticoIcons.KENTICO)]

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Licenses;

public class LicensesWidget(LicensesFacade licensesFacade, AssetItemService itemService) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.LicensesWidget";
    public const string NAME = "3rd Party Licenses";

    private readonly LicensesFacade licensesFacade = licensesFacade;
    private readonly AssetItemService itemService = itemService;

    public async Task<IViewComponentResult> InvokeAsync(LicensesWidgetProperties properties) =>
        await Validate(properties)
            .Match(
                vm => View("~/Components/PageBuilder/Widgets/LicensesWidget/LicensesWidget.cshtml", vm),
                vm => View("~/Components/ComponentError.cshtml", vm)
            );

    private async Task<Result<LicensesWidgetViewModel, ComponentErrorViewModel>> Validate(LicensesWidgetProperties props)
    {
        var relatedAsset = props.LicenseFiles.FirstOrDefault();

        if (relatedAsset is null)
        {
            return Result.Failure<LicensesWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "No License File has been selected."));
        }

        var asset = await itemService.RetrieveMediaFile(props.LicenseFiles.FirstOrDefault());

        if (asset is null)
        {
            return Result.Failure<LicensesWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, $"Could not find the License File {relatedAsset.Name}: {relatedAsset.Identifier}"));
        }

        var licenseTypeLinks = GetDictionary(props.LicensesTypeDescriptionLinks);
        var licenses = await licensesFacade.GetLicenses(asset, licenseTypeLinks);
        return new LicensesWidgetViewModel(props, licenses);
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

public class LicensesWidgetProperties : BaseWidgetProperties
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
    public IEnumerable<AssetRelatedItem> LicenseFiles { get; set; } = [];

    [TextAreaComponent(Label = "Licenses type description links", Order = 4)]
    public string LicensesTypeDescriptionLinks { get; set; } = "";
}

public class LicensesWidgetViewModel
{
    public string Title { get; }
    public string NoLicensesText { get; }
    public LicensesViewModel Licenses { get; }

    public LicensesWidgetViewModel(LicensesWidgetProperties props, LicensesViewModel licenses)
    {
        Title = props.Title;
        NoLicensesText = props.NoLicensesText;
        Licenses = licenses;
    }
}
