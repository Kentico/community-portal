using System.ComponentModel;
using Kentico.Community.Portal.Web.Components.Widgets.CTAButton;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Websites.FormAnnotations;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWidget(
    identifier: "CommunityPortal.CtaButtonWidget",
    name: "CTA button",
    viewComponentType: typeof(CTAButtonWidgetViewComponent),
    propertiesType: typeof(CTAButtonWidgetProperties),
    Description = "Call to action button with configurable target page.",
    IconClass = "icon-rectangle-a",
    AllowCache = true)]

namespace Kentico.Community.Portal.Web.Components.Widgets.CTAButton;

public class CTAButtonWidgetViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(ComponentViewModel<CTAButtonWidgetProperties> vm)
    {
        var model = new CTAButtonWidgetViewModel(vm.Properties);

        return View("~/Components/Widgets/CTAButton/CTAButton.cshtml", model);
    }
}

public class CTAButtonWidgetProperties : IWidgetProperties
{
    [TextInputComponent(
        Label = "Link Text",
        Order = 1)]
    public string Text { get; set; } = "";

    [UrlSelectorComponent(
        Label = "Link URL",
        Order = 2)]
    public string LinkURL { get; set; } = "";

    [DropDownComponent(
        Label = "Alignment",
        ExplanationText = "The alignment of the CTA",
        Tooltip = "Select an alignment",
        DataProviderType = typeof(EnumDropDownOptionsProvider<HorizontalAlignments>),
        Order = 3
    )]
    public string HorizontalAlignment { get; set; } = nameof(HorizontalAlignments.Left);
    public HorizontalAlignments HorizontalAlignmentParsed => EnumDropDownOptionsProvider<HorizontalAlignments>.Parse(HorizontalAlignment, HorizontalAlignments.Left);
}

public enum HorizontalAlignments
{
    [Description("Left")]
    Left,
    [Description("Center")]
    Center
}

public class CTAButtonWidgetViewModel(CTAButtonWidgetProperties props)
{
    public string Text { get; set; } = props.Text;
    public string LinkURL { get; set; } = props.LinkURL;
    public HorizontalAlignments HorizontalAlignment { get; set; } = props.HorizontalAlignmentParsed;
}
