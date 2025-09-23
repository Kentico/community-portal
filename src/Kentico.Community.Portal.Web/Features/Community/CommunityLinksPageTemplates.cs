using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Features.Community;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using Kentico.Xperience.Admin.Base.FormAnnotations;
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
    IContentRetriever contentRetriever,
    WebPageMetaService metaService) : Controller
{
    private readonly IContentRetriever contentRetriever = contentRetriever;
    private readonly WebPageMetaService metaService = metaService;

    public async Task<ActionResult> Index()
    {
        var page = await contentRetriever.RetrieveCurrentPage<CommunityLinksPage>();
        if (page is null)
        {
            return NotFound();
        }

        metaService.SetMeta(page);

        return new TemplateResult(new CommunityLinksPageViewModel(page));
    }
}

public record CommunityLinksPageViewModel(CommunityLinksPage Page);
