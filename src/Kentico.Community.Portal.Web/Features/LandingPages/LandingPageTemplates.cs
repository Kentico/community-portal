using Kentico.Community.Portal.Core.Components;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets;
using Kentico.Community.Portal.Web.Features.LandingPages;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Mvc;

using Icons = Kentico.Xperience.Admin.Base.Icons;

[assembly: RegisterPageTemplate(
    identifier: "KenticoCommunity.LandingPage_Default",
    name: "Landing Page - Default",
    propertiesType: typeof(LandingPageDefaultTemplateProperties),
    customViewName: "~/Features/LandingPages/LandingPage_Default.cshtml",
    ContentTypeNames = [LandingPage.CONTENT_TYPE_NAME],
    Description = "Default Landing Page template with a heading",
    IconClass = Icons.LHeaderText
)]

[assembly: RegisterPageTemplate(
    identifier: "KenticoCommunity.LandingPage_Empty",
    name: "Landing Page - Empty",
    propertiesType: typeof(LandingPageEmptyTemplateProperties),
    customViewName: "~/Features/LandingPages/LandingPage_Empty.cshtml",
    ContentTypeNames = [LandingPage.CONTENT_TYPE_NAME],
    Description = "Landing Page template with no content and a single Editable Area",
    IconClass = Icons.LText
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: LandingPage.CONTENT_TYPE_NAME,
    controllerType: typeof(LandingPageTemplateController)
)]

namespace Kentico.Community.Portal.Web.Features.LandingPages;

public class LandingPageTemplateProperties : IPageTemplateProperties
{
    [RadioGroupComponent(
        Label = "Widget selection",
        ExplanationText = "Controls which widgets are available in the editable area",
        Options = "Standard;Standard\nFull;Full",
        Order = 1
    )]
    public string WidgetSelection { get; set; } = nameof(LandingPageWidgetSelections.Standard);

    /// <summary>
    /// Parsed version of <see cref="WidgetSelection" /> as a <see cref="LandingPageWidgetSelections" /> enum value.
    /// Defaults to <see cref="LandingPageWidgetSelections.Standard" /> when parsing fails.
    /// </summary>
    public LandingPageWidgetSelections WidgetSelectionParsed =>
        EnumDropDownOptionsProvider<LandingPageWidgetSelections>.Parse(WidgetSelection, LandingPageWidgetSelections.Standard);

    public static readonly string[] StandardWidgets =
    [
        WidgetIdentifiers.BlogPostList,
        WidgetIdentifiers.Carousel,
        WidgetIdentifiers.CommunityGroupCard,
        WidgetIdentifiers.CTAButton,
        WidgetIdentifiers.Embed,
        WidgetIdentifiers.FallbackForm,
        WidgetIdentifiers.FAQ,
        WidgetIdentifiers.File,
        WidgetIdentifiers.Form,
        WidgetIdentifiers.Heading,
        WidgetIdentifiers.Image,
        WidgetIdentifiers.Integration,
        WidgetIdentifiers.LinkList,
        WidgetIdentifiers.Markdown,
        WidgetIdentifiers.PageHeading,
        WidgetIdentifiers.Poll,
        WidgetIdentifiers.ProfileCard,
        WidgetIdentifiers.Testimonial,
        WidgetIdentifiers.Video
    ];
}

public enum LandingPageWidgetSelections { Standard, Full }

public class LandingPageDefaultTemplateProperties : LandingPageTemplateProperties { }
public class LandingPageEmptyTemplateProperties : LandingPageTemplateProperties { }

public class LandingPageTemplateController(
    IContentRetriever contentRetriever,
    WebPageMetaService metaService) : Controller
{
    private readonly IContentRetriever contentRetriever = contentRetriever;
    private readonly WebPageMetaService metaService = metaService;

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// <remarks>
    /// This authorization filter must be applied at the Controller level because it interfers
    /// with the Page Builder if registered globally
    /// </remarks>
    [TypeFilter(typeof(ContentAuthorizationFilter))]
    public async Task<ActionResult> Index()
    {
        var landingPage = await contentRetriever.RetrieveCurrentPage<LandingPage>();
        if (landingPage is null)
        {
            return NotFound();
        }

        metaService.SetMeta(landingPage);

        return new TemplateResult(landingPage);
    }
}
