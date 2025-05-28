using Kentico.Community.Portal.Core.Emails;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.EmailBuilder.Widgets.PrimaryContent;
using Kentico.EmailBuilder.Web.Mvc;
using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailWidget(
    identifier: PrimaryContentWidget.IDENTIFIER,
    name: "Primary Content",
    componentType: typeof(PrimaryContentWidget),
    PropertiesType = typeof(PrimaryContentWidgetProperties),
    IconClass = KenticoIcons.PARAGRAPH_CENTER,
    Description = "The primary text content field of the current email content type")]

namespace Kentico.Community.Portal.Web.Components.EmailBuilder.Widgets.PrimaryContent;

public partial class PrimaryContentWidget : ComponentBase
{
    public const string IDENTIFIER = "KenticoCommunity.EmailBuilder.Widgets.PrimaryContent";

    private EmailContext? emailContext;

    [Inject] public required IEmailContextAccessor EmailContextAccessor { get; set; }

    [Parameter] public PrimaryContentWidgetProperties Properties { get; set; } = null!;

    public Maybe<MarkupString> Model { get; set; }
    public EmailContext EmailContext => emailContext ??= EmailContextAccessor.GetContext();

    protected override async Task OnInitializedAsync()
    {
        var context = EmailContextAccessor.GetContext();
        var email = await context.GetEmail<AutoresponderEmail>();
        if (email is null)
        {
            return;
        }

        Model = Maybe
            .From(email.AutoresponderEmailBodyContent)
            .MapNullOrWhiteSpaceAsNone()
            .Map(c => new MarkupString(c));
    }
}

public class PrimaryContentWidgetProperties : IEmailWidgetProperties { }
