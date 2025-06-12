using System.ComponentModel;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.CTAButton;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Websites.FormAnnotations;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWidget(
    identifier: CTAButtonWidget.IDENTIFIER,
    name: "CTA button",
    viewComponentType: typeof(CTAButtonWidget),
    propertiesType: typeof(CTAButtonWidgetProperties),
    Description = "Call to action button with configurable target page.",
    IconClass = KenticoIcons.RECTANGLE_A,
    AllowCache = true)]

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.CTAButton;

public class CTAButtonWidget : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.CtaButtonWidget";
    public IViewComponentResult Invoke(ComponentViewModel<CTAButtonWidgetProperties> vm)
    {
        var model = new CTAButtonWidgetViewModel(vm.Properties);

        return View("~/Components/PageBuilder/Widgets/CTAButton/CTAButton.cshtml", model);
    }
}

public class CTAButtonWidgetProperties : BaseWidgetProperties
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
