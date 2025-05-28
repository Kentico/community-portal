using Kentico.Community.Portal.Core.Emails;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.EmailBuilder.Widgets.Spacer;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailWidget(
    identifier: SpacerWidget.IDENTIFIER,
    name: "Spacer",
    componentType: typeof(SpacerWidget),
    PropertiesType = typeof(SpacerWidgetProperties),
    IconClass = KenticoIcons.BUILDING_BLOCK,
    Description = "Adds customizable space between widgets")]

namespace Kentico.Community.Portal.Web.Components.EmailBuilder.Widgets.Spacer;

public partial class SpacerWidget : ComponentBase
{
    public const string IDENTIFIER = "KenticoCommunity.EmailBuilder.Widgets.Spacer";

    private EmailContext? emailContext;

    [Inject] public required IEmailContextAccessor EmailContextAccessor { get; set; }

    [Parameter] public SpacerWidgetProperties Properties { get; set; } = null!;

    public EmailContext EmailContext => emailContext ??= EmailContextAccessor.GetContext();

    protected override async Task OnInitializedAsync()
    {
        var context = EmailContextAccessor.GetContext();
        var email = await context.GetEmail<AutoresponderEmail>();
        if (email is null)
        {
            return;
        }
    }
}

public class SpacerWidgetProperties : IEmailWidgetProperties
{
    [DropDownComponent(
        Label = "Space size",
        ExplanationText = "The size of the spacer",
        Tooltip = "Select a size",
        DataProviderType = typeof(EnumDropDownOptionsProvider<SpaceSizes>),
        Order = 1
    )]
    public string SpaceSize { get; set; } = nameof(SpaceSizes.Medium);
    public SpaceSizes SpaceSizeParsed => EnumDropDownOptionsProvider<SpaceSizes>.Parse(SpaceSize, SpaceSizes.Medium);
}

public enum SpaceSizes { Small, Medium, Large }
