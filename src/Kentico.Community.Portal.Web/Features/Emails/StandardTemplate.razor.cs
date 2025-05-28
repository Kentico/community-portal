using CMS.EmailMarketing;
using Kentico.Community.Portal.Core.Emails;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Features.Emails;
using Kentico.EmailBuilder.Web.Mvc;
using Microsoft.AspNetCore.Components;
using static Kentico.Community.Portal.Web.Components.EmailBuilder.EmailFooter;
using static Kentico.Community.Portal.Web.Components.EmailBuilder.EmailHeader;

[assembly: RegisterEmailTemplate(
    identifier: StandardTemplate.IDENTIFIER,
    name: "Standard",
    componentType: typeof(StandardTemplate),
    Description = "Supports full Email Builder.",
    IconClass = KenticoIcons.LAYOUTS,
    ContentTypeNames = [AutoresponderEmail.CONTENT_TYPE_NAME])]

namespace Kentico.Community.Portal.Web.Features.Emails;

public partial class StandardTemplate : ComponentBase
{
    public const string IDENTIFIER = "KenticoCommunity.EmailBuilder.Templates.Standard";

    [Inject] public required IEmailContextAccessor EmailContextAccessor { get; set; }
    [Inject] public required IEmailRecipientContextAccessor RecipientContextAccessor { get; set; }

    public Maybe<StandardTemplateViewModel> Model { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var context = EmailContextAccessor.GetContext();
        var email = await context.GetEmail<AutoresponderEmail>();
        if (email is null || email.EmailDesignEmailDefaultsContent.FirstOrDefault() is null)
        {
            return;
        }

        var recipient = RecipientContextAccessor.GetContext();

        Model = Maybe.From(new StandardTemplateViewModel(email, recipient));
    }
}

public class StandardTemplateViewModel
{
    public string Title { get; }
    public string PreviewText { get; }
    public MarkupString Content { get; }
    public UTMParametersDataType UTMParameters { get; }
    public EmailHeaderViewModel Header { get; }
    public EmailFooterViewModel Footer { get; }

    public EmailDefaultsContent Defaults { get; }

    public StandardTemplateViewModel(AutoresponderEmail email, EmailRecipientContext recipient)
    {
        Title = email.AutoresponderEmailTitle;
        PreviewText = email.EmailPreviewText;
        Content = new(email.AutoresponderEmailBodyContent);
        UTMParameters = email.AutoresponderEmailUTMParameters;
        Defaults = email.EmailDesignEmailDefaultsContent.First();

        var logoLight = Defaults.EmailDefaultsContentLogoLightImageContent.TryFirst();
        var logoDark = Defaults.EmailDefaultsContentLogoDarkImageContent.TryFirst();
        string logoLinkURL = CampaignManagement.ReplaceUtmParameters(Defaults.EmailDefaultsContentLogoLinkURL, UTMParameters);
        Header = new(Title, logoLight, logoDark, logoLinkURL);
        Footer = new(recipient, false, logoLight, logoDark, logoLinkURL, Defaults.EmailDefaultsContentSocialLinks);
    }
}
