using Kentico.Community.Portal.Core.Content;
using Kentico.Community.Portal.Web.Features.Support;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;

[assembly: RegisterPageTemplate(
    identifier: "KenticoCommunity.SupportPage_Default",
    name: "Support Page - Default",
    propertiesType: typeof(SupportPageTemplateProperties),
    customViewName: "~/Features/Support/SupportPage_Default.cshtml",
    ContentTypeNames = new[] { SupportPage.CONTENT_TYPE_NAME },
    Description = "",
    IconClass = ""
)]

namespace Kentico.Community.Portal.Web.Features.Support;

public class SupportPageTemplateProperties : IPageTemplateProperties { }
