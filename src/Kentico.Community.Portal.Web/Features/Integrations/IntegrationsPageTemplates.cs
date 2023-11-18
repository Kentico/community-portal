using Kentico.Community.Portal.Web.Features.Integrations;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;

[assembly: RegisterPageTemplate(
    identifier: "KenticoCommunity.IntegrationsLandingPage_Default",
    name: "Integrations Landing Page - Default",
    propertiesType: typeof(IntegrationsLandingPageTemplateProperties),
    customViewName: "~/Features/Integrations/IntegrationsLandingPage_Default.cshtml",
    ContentTypeNames = new[] { IntegrationsLandingPage.CONTENT_TYPE_NAME },
    Description = "",
    IconClass = ""
)]

namespace Kentico.Community.Portal.Web.Features.Integrations;

public class IntegrationsLandingPageTemplateProperties : IPageTemplateProperties { }
