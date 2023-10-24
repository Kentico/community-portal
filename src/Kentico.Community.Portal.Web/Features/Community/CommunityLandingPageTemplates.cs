using Kentico.Community.Portal.Web.Features.Community;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;

[assembly: RegisterPageTemplate(
    identifier: "KenticoCommunity.CommunityLandingPage_Default",
    name: "Community Page - Default",
    propertiesType: typeof(CommunityLandingPageTemplateProperties),
    customViewName: "~/Features/Community/CommunityLandingPage_Default.cshtml",
    ContentTypeNames = new[] { CommunityLandingPage.CONTENT_TYPE_NAME },
    Description = "",
    IconClass = ""
)]

namespace Kentico.Community.Portal.Web.Features.Community;

public class CommunityLandingPageTemplateProperties : IPageTemplateProperties { }
