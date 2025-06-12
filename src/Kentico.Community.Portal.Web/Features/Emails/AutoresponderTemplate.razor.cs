using CMS.EmailMarketing;
using Kentico.Community.Portal.Core.Emails;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Features.Emails;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Components;
using static Kentico.Community.Portal.Web.Components.EmailBuilder.EmailFooter;
using static Kentico.Community.Portal.Web.Components.EmailBuilder.EmailHeader;

[assembly: RegisterEmailTemplate(
    identifier: AutoresponderTemplate.IDENTIFIER,
    name: "Autoresponder",
    componentType: typeof(AutoresponderTemplate),
    PropertiesType = typeof(AutoresponderTemplateProperties),
    ContentTypeNames = [AutoresponderEmail.CONTENT_TYPE_NAME])]

namespace Kentico.Community.Portal.Web.Features.Emails;

public partial class AutoresponderTemplate : ComponentBase
{
    public const string IDENTIFIER = "KenticoCommunity.EmailBuilder.Templates.Autoresponder";

    [Inject] public required IEmailContextAccessor EmailContextAccessor { get; set; }
    [Inject] public required IEmailRecipientContextAccessor RecipientContextAccessor { get; set; }

    [Parameter] public AutoresponderTemplateProperties Properties { get; set; } = new();

    public Maybe<AutoresponderTemplateViewModel> Model { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var context = EmailContextAccessor.GetContext();
        var email = await context.GetEmail<AutoresponderEmail>();
        if (email is null || email.EmailDesignEmailDefaultsContent.FirstOrDefault() is null)
        {
            return;
        }

        var recipient = RecipientContextAccessor.GetContext();

        Model = Maybe.From(new AutoresponderTemplateViewModel(email, recipient));
    }
}

public class AutoresponderTemplateViewModel
{
    public string Title { get; }
    public string PreviewText { get; }
    public MarkupString Content { get; }
    public UTMParametersDataType UTMParameters { get; }
    public EmailHeaderViewModel Header { get; }
    public EmailFooterViewModel Footer { get; }

    public EmailDefaultsContent Defaults { get; }

    public AutoresponderTemplateViewModel(AutoresponderEmail email, EmailRecipientContext recipient)
    {
        Title = email.AutoresponderEmailTitle;
        PreviewText = email.EmailPreviewText;
        Content = new(email.AutoresponderEmailBodyContentHTML);
        UTMParameters = email.AutoresponderEmailUTMParameters;
        Defaults = email.EmailDesignEmailDefaultsContent.First();

        var logoLight = Defaults.EmailDefaultsContentLogoLightImageContent.TryFirst();
        var logoDark = Defaults.EmailDefaultsContentLogoDarkImageContent.TryFirst();
        string logoLinkURL = CampaignManagement.ReplaceUtmParameters(Defaults.EmailDefaultsContentLogoLinkURL, UTMParameters);
        Header = new(Title, logoLight, logoDark, logoLinkURL);
        Footer = new(recipient, false, logoLight, logoDark, logoLinkURL, Defaults.EmailDefaultsContentSocialLinks);
    }
}

public class AutoresponderTemplateProperties : IEmailTemplateProperties
{
    [DropDownComponent(
        Label = "Design",
        ExplanationText = "The design of the template",
        DataProviderType = typeof(EnumDropDownOptionsProvider<ContentDesigns>),
        Tooltip = "Select a design",
        Order = 1
    )]
    public string Design { get; set; } = nameof(ContentDesigns.Normal);
    public ContentDesigns DesignParsed => EnumDropDownOptionsProvider<ContentDesigns>.Parse(Design, ContentDesigns.Normal);
}

public enum ContentDesigns { Normal, Card }
