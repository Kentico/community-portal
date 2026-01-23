using CMS.ContentEngine;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.EmailBuilder.Widgets.RichText;
using Kentico.EmailBuilder.Web.Mvc;
using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailWidget(
    identifier: RichTextWidget.IDENTIFIER,
    name: "Rich text",
    componentType: typeof(RichTextWidget),
    IconClass = KenticoIcons.RECTANGLE_A,
    PropertiesType = typeof(RichTextWidgetProperties),
    Description = "Basic rich text content")]

namespace Kentico.Community.Portal.Web.Components.EmailBuilder.Widgets.RichText;

public partial class RichTextWidget : ComponentBase
{
    public const string IDENTIFIER = "KenticoCommunity.EmailBuilder.Widgets.RichText";

    [Inject] public required IEmailContextAccessor EmailContextAccessor { get; set; }

    [Parameter] public RichTextWidgetProperties Properties { get; set; } = null!;

    public MarkupString RenderedText => new(Properties.Text);
    public EmailContext EmailContext => field ??= EmailContextAccessor.GetContext();

}

public class RichTextWidgetProperties : IEmailWidgetProperties
{
    [TrackContentItemReference(typeof(ContentItemReferenceExtractor))]
    public string Text { get; set; } = "";
}

