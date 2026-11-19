using CMS.ContentEngine;
using Kentico.Community.Portal.Web.Components.PageBuilder.Sections.Unstyled;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets;
using Kentico.Community.Portal.Web.Features.Home;
using Kentico.Community.Portal.Web.Features.SEO.StructuredData;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Mvc;

using Icons = Kentico.Xperience.Admin.Base.Icons;

[assembly: RegisterPageTemplate(
    identifier: "KenticoCommunity.HomePage_Default",
    name: "Home Page - Default",
    propertiesType: typeof(HomePageTemplateProperties),
    customViewName: "~/Features/Home/HomePage_Default.cshtml",
    ContentTypeNames = [HomePage.CONTENT_TYPE_NAME],
    Description = "",
    IconClass = Icons.Home
)]

[assembly: RegisterPageTemplate(
    identifier: HomePageHeroTemplateProperties.IDENTIFIER,
    name: "Home Page - Hero",
    propertiesType: typeof(HomePageHeroTemplateProperties),
    customViewName: "~/Features/Home/HomePage_Hero.cshtml",
    ContentTypeNames = [HomePage.CONTENT_TYPE_NAME],
    Description = "Home Page template with a hero banner, overlapping quick links area and a main Editable Area",
    IconClass = Icons.LHeaderText
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: HomePage.CONTENT_TYPE_NAME,
    controllerType: typeof(HomePageTemplateController)
)]

namespace Kentico.Community.Portal.Web.Features.Home;

public class HomePageTemplateProperties : IPageTemplateProperties { }

public class HomePageHeroTemplateProperties : IPageTemplateProperties
{
    public const string IDENTIFIER = "KenticoCommunity.HomePage_Hero";

    /// <summary>
    /// Widgets allowed in the quick links area that overlaps the hero.
    /// </summary>
    public static readonly string[] HeroLinksWidgets = [WidgetIdentifiers.LinkList];

    /// <summary>
    /// Sections allowed in the quick links area that overlaps the hero.
    /// </summary>
    public static readonly string[] HeroLinksSections = [UnstyledSection.IDENTIFIER];

    [ContentItemSelectorComponent(
        contentTypeName: ImageContent.CONTENT_TYPE_NAME,
        Label = "Hero image",
        ExplanationText = "Image displayed beside the page title. When empty, the hero spans the full width.",
        MinimumItems = 0,
        MaximumItems = 1,
        Order = 1)]
    public IEnumerable<ContentItemReference> HeroImage { get; set; } = [];
}

public class HomePageTemplateController(
    IContentRetriever contentRetriever,
    WebPageMetaService metaService,
    JsonLdCollector jsonLd) : Controller
{
    private readonly IContentRetriever contentRetriever = contentRetriever;
    private readonly WebPageMetaService metaService = metaService;
    private readonly JsonLdCollector jsonLd = jsonLd;

    public async Task<ActionResult> Index()
    {
        var homePage = await contentRetriever.RetrieveCurrentPage<HomePage>();
        if (homePage is null)
        {
            return NotFound();
        }

        metaService.SetMeta(homePage);

        // Header logo (SiteBrand) links to the site root
        jsonLd.Add(new OrganizationJsonLd(
            Name: SiteBrand.Name,
            URL: "/".RelativePathToAbsoluteURL(Request),
            Logo: SiteBrand.LogoPath.RelativePathToAbsoluteURL(Request)));

        return new TemplateResult(homePage);
    }
}
