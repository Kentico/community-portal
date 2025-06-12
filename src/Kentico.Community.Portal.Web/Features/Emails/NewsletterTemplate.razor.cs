using AngleSharp.Common;
using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.EmailMarketing;
using CMS.Membership;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Emails;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Features.Emails;
using Kentico.Community.Portal.Web.Membership;
using Kentico.EmailBuilder.Web.Mvc;
using Microsoft.AspNetCore.Components;
using static Kentico.Community.Portal.Web.Components.EmailBuilder.EmailFooter;
using static Kentico.Community.Portal.Web.Components.EmailBuilder.EmailHeader;

[assembly: RegisterEmailTemplate(
    identifier: NewsletterTemplate.IDENTIFIER,
    name: "Newsletter",
    componentType: typeof(NewsletterTemplate),
    Description = "Default template for the Kentico Community Newsletter",
    IconClass = KenticoIcons.NEWSPAPER,
    ContentTypeNames = [NewsletterEmail.CONTENT_TYPE_NAME])]

namespace Kentico.Community.Portal.Web.Features.Emails;

public partial class NewsletterTemplate : ComponentBase
{
    [Inject] public required IEmailContextAccessor EmailContextAccessor { get; set; }
    [Inject] public required ITaxonomyRetriever TaxonomyRetriever { get; set; }
    [Inject] public required IInfoProvider<MemberInfo> MemberProvider { get; set; }
    [Inject] public required IEmailRecipientContextAccessor RecipientContextAccessor { get; set; }

    public Maybe<NewsletterTemplateViewModel> Model { get; set; }

    public const string IDENTIFIER = "KenticoCommunity.EmailBuilder.Templates.Newsletter";

    protected override async Task OnInitializedAsync()
    {
        var context = EmailContextAccessor.GetContext();
        var email = await context.GetEmail<NewsletterEmail>();
        if (email is null || email.EmailDesignEmailDefaultsContent.FirstOrDefault() is null)
        {
            return;
        }

        var blogTypes = (await TaxonomyRetriever.RetrieveTaxonomy(SystemTaxonomies.BlogType.TaxonomyName, PortalWebSiteChannel.DEFAULT_LANGUAGE))
            .Tags
            .ToDictionary(t => t.Identifier);

        var dxTopics = (await TaxonomyRetriever.RetrieveTaxonomy(SystemTaxonomies.DXTopic.TaxonomyName, PortalWebSiteChannel.DEFAULT_LANGUAGE))
            .Tags
            .ToDictionary(t => t.Identifier);

        var members = await MemberProvider.Get()
        .WhereIn(nameof(MemberInfo.MemberID), email.NewsletterEmailLinkContents.Select(l => l.LinkContentMemberID))
        .GetEnumerableTypedResultAsync();

        var communityMembers = members
        .Select(CommunityMember.FromMemberInfo)
        .ToDictionary(m => m.Id);

        var recipient = RecipientContextAccessor.GetContext();

        Model = Maybe.From(new NewsletterTemplateViewModel(email, recipient, blogTypes, dxTopics, communityMembers));
    }
}

public class NewsletterTemplateViewModel
{
    public string Title { get; }
    public string IssueNumber { get; }
    public DateTime PublishedDate { get; }
    public string PreviewText { get; }
    public MarkupString IntroContent { get; }
    public IEnumerable<BlogPostPage> BlogPostPages { get; set; }
    public IEnumerable<LinkContent> LinkContents { get; set; }
    public IEnumerable<QAndAQuestionPage> Discussions { get; set; }
    public MarkupString OutroContent { get; }
    public UTMParametersDataType UTMParameters { get; }
    public EmailHeaderViewModel Header { get; }
    public EmailFooterViewModel Footer { get; }

    public EmailDefaultsContent Defaults { get; }
    public Dictionary<Guid, Tag> BlogTypes { get; }
    public Dictionary<Guid, Tag> DXTopics { get; }
    public Dictionary<int, CommunityMember> Members { get; }

    public NewsletterTemplateViewModel(
        NewsletterEmail email,
        EmailRecipientContext recipient,
        Dictionary<Guid, Tag> blogTypes,
        Dictionary<Guid, Tag> dxTopics,
        Dictionary<int, CommunityMember> members)
    {
        Title = email.NewsletterEmailTitle;
        IssueNumber = email.NewsletterEmailIssueNumber;
        PublishedDate = email.NewsletterEmailPublishedDate;
        PreviewText = email.EmailPreviewText;
        IntroContent = new(email.NewsletterEmailIntroContentHTML);
        BlogPostPages = email.NewsletterEmailBlogPostPages;
        LinkContents = email.NewsletterEmailLinkContents;
        Discussions = email.NewsletterEmailQAndADiscussionPages;
        OutroContent = new(email.NewsletterEmailOutroContentHTML);
        UTMParameters = email.NewsletterEmailUTMParameters;
        Defaults = email.EmailDesignEmailDefaultsContent.First();

        var logoLight = Defaults.EmailDefaultsContentLogoLightImageContent.TryFirst();
        var logoDark = Defaults.EmailDefaultsContentLogoDarkImageContent.TryFirst();
        string logoLinkURL = CampaignManagement.ReplaceUtmParameters(Defaults.EmailDefaultsContentLogoLinkURL, UTMParameters);
        Header = new(Title, logoLight, logoDark, logoLinkURL, $"Issue #{IssueNumber}");
        Footer = new(recipient, true, logoLight, logoDark, logoLinkURL, Defaults.EmailDefaultsContentSocialLinks);

        BlogTypes = blogTypes;
        DXTopics = dxTopics;
        Members = members;
    }
}
