using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using Kentico.Community.Portal.Core.Content;
using Kentico.Community.Portal.Web.Features.LandingPages;

[assembly: RegisterPageTemplate(
    identifier: "KenticoCommunity.LandingPage_Default",
    name: "Landing Page - Default",
    propertiesType: typeof(LandingPageDefaultTemplateProperties),
    customViewName: "~/Features/LandingPages/LandingPage_Default.cshtml",
    ContentTypeNames = new[] { LandingPage.CONTENT_TYPE_NAME },
    Description = "Default Landing Page template with a heading",
    IconClass = "xp-l-header-text"
)]

[assembly: RegisterPageTemplate(
    identifier: "KenticoCommunity.LandingPage_Empty",
    name: "Landing Page - Empty",
    propertiesType: typeof(LandingPageEmptyTemplateProperties),
    customViewName: "~/Features/LandingPages/LandingPage_Empty.cshtml",
    ContentTypeNames = new[] { LandingPage.CONTENT_TYPE_NAME },
    Description = "Landing Page template with no content and a single Editable Area",
    IconClass = "xp-l-text"
)]

namespace Kentico.Community.Portal.Web.Features.LandingPages;

public class LandingPageTemplateProperties : IPageTemplateProperties { }
public class LandingPageDefaultTemplateProperties : LandingPageTemplateProperties { }
public class LandingPageEmptyTemplateProperties : LandingPageTemplateProperties { }
