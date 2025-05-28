using System.ComponentModel;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.EmailBuilder.Widgets.CTA;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailWidget(
    identifier: CTAWidget.IDENTIFIER,
    name: "CTA",
    componentType: typeof(CTAWidget),
    IconClass = KenticoIcons.CHAIN,
    PropertiesType = typeof(CTAWidgetProperties),
    Description = "Call to action with a label, link, and design customization.")]

namespace Kentico.Community.Portal.Web.Components.EmailBuilder.Widgets.CTA;

public partial class CTAWidget : ComponentBase
{
    public const string IDENTIFIER = "KenticoCommunity.EmailBuilder.Widgets.CTA";

    private EmailContext? emailContext;

    [Inject] public required IEmailContextAccessor EmailContextAccessor { get; set; }

    [Parameter]
    public required CTAWidgetProperties Properties { get; set; }

    public EmailContext EmailContext => emailContext ??= EmailContextAccessor.GetContext();
}

public class CTAWidgetProperties : IEmailWidgetProperties
{
    [TextInputComponent(
        Label = "Label",
        Order = 1)]
    public string Label { get; set; } = "";

    [TextInputComponent(
        Label = "URL",
        Order = 2)]
    public string URL { get; set; } = "";

    [DropDownComponent(
        Label = "Alignment",
        ExplanationText = "The alignment of the CTA",
        Tooltip = "Select an alignment",
        DataProviderType = typeof(EnumDropDownOptionsProvider<CTAAlignments>),
        Order = 3
    )]
    public string Alignment { get; set; } = nameof(CTAAlignments.Center);
    public CTAAlignments AlignmentParsed => EnumDropDownOptionsProvider<CTAAlignments>.Parse(Alignment, CTAAlignments.Center);

    [DropDownComponent(
        Label = "Padding",
        ExplanationText = "The padding of the CTA container",
        Tooltip = "Select a padding",
        DataProviderType = typeof(EnumDropDownOptionsProvider<CTAPaddings>),
        Order = 4
    )]
    public string Padding { get; set; } = nameof(CTAPaddings.Medium);
    public CTAPaddings PaddingParsed => EnumDropDownOptionsProvider<CTAPaddings>.Parse(Padding, CTAPaddings.Medium);

    [DropDownComponent(
        Label = "Button Size",
        ExplanationText = "The size of the CTA button",
        Tooltip = "Select a button size",
        DataProviderType = typeof(EnumDropDownOptionsProvider<CTAButtonSizes>),
        Order = 5
    )]
    public string ButtonSize { get; set; } = nameof(CTAButtonSizes.Medium);
    public CTAButtonSizes ButtonSizeParsed => EnumDropDownOptionsProvider<CTAButtonSizes>.Parse(ButtonSize, CTAButtonSizes.Medium);

    [DropDownComponent(
        Label = "Button Color",
        ExplanationText = "The color of the CTA button",
        Tooltip = "Select a button color",
        DataProviderType = typeof(EnumDropDownOptionsProvider<CTAButtonColors>),
        Order = 6
    )]
    public string ButtonColor { get; set; } = nameof(CTAButtonColors.Primary);
    public CTAButtonColors ButtonColorParsed => EnumDropDownOptionsProvider<CTAButtonColors>.Parse(ButtonColor, CTAButtonColors.Primary);

    [DropDownComponent(
        Label = "Border Radius",
        ExplanationText = "The border radius of the CTA button",
        Tooltip = "Select a border radius",
        DataProviderType = typeof(EnumDropDownOptionsProvider<CTABorderRadii>),
        Order = 7
    )]
    public string BorderRadius { get; set; } = nameof(CTABorderRadii.Square);
    public CTABorderRadii BorderRadiusParsed => EnumDropDownOptionsProvider<CTABorderRadii>.Parse(BorderRadius, CTABorderRadii.Square);
}

public enum CTAAlignments { Left, Center, Right }
public enum CTAPaddings { Small, Medium, Large }
public enum CTAButtonSizes { Small, Medium, Large }
public enum CTAButtonColors { Primary, Secondary, [Description("Light and dark")] LightAndDark }
public enum CTABorderRadii { Square, Rounded, Pill }
