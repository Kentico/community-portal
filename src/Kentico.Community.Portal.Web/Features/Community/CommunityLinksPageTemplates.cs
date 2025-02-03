using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Features.Community;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterPageTemplate(
    identifier: "KenticoCommunity.CommunityLinksPage_Default",
    name: "Community Links Page - Default",
    propertiesType: typeof(CommunityLinksPageTemplateProperties),
    customViewName: "~/Features/Community/CommunityLinksPage_Default.cshtml",
    ContentTypeNames = [CommunityLinksPage.CONTENT_TYPE_NAME],
    Description = "",
    IconClass = KenticoIcons.TABLE
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: CommunityLinksPage.CONTENT_TYPE_NAME,
    controllerType: typeof(CommunityLinksPageTemplateController)
)]

namespace Kentico.Community.Portal.Web.Features.Community;

public class CommunityLinksPageTemplateProperties : IPageTemplateProperties
{
    [NumberInputComponent(
        Label = "Link publish delay (days)",
        ExplanationText = "The number of days delay before displaying published links.",
        Order = 1
    )]
    public int LinkPublishDelayDays { get; set; } = 14;

}

public class CommunityLinksPageTemplateController(
    IMediator mediator,
    WebPageMetaService metaService,
    IWebPageDataContextRetriever contextRetriever,
    IPageTemplatePropertiesRetriever propertiesRetriever) : Controller
{
    private readonly IMediator mediator = mediator;
    private readonly WebPageMetaService metaService = metaService;
    private readonly IWebPageDataContextRetriever contextRetriever = contextRetriever;
    private readonly IPageTemplatePropertiesRetriever propertiesRetriever = propertiesRetriever;

    public async Task<ActionResult> Index()
    {
        if (!contextRetriever.TryRetrieve(out var data))
        {
            return NotFound();
        }

        var page = await mediator.Send(new CommunityLinksPageQuery(data.WebPage));

        metaService.SetMeta(new(page));

        var props = propertiesRetriever.Retrieve<CommunityLinksPageTemplateProperties>();
        var resp = await mediator.Send(new LinkContentsCommunityQuery(props.LinkPublishDelayDays));

        return new TemplateResult(new CommunityLinksPageViewModel(page, [.. resp.Select(i => new CommunityLinkViewModel(i.Content, i.Member))]));
    }
}

public record CommunityLinksPageViewModel(CommunityLinksPage Page, IReadOnlyList<CommunityLinkViewModel> Links);
public record CommunityLinkViewModel(LinkContent Content, Maybe<CommunityMember> Member);
