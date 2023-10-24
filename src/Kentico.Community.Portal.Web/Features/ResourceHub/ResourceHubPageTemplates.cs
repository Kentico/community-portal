using Kentico.Community.Portal.Core.Content;
using Kentico.Community.Portal.Web.Features.ResourceHub;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;

[assembly: RegisterPageTemplate(
    identifier: "KenticoCommunity.ResourceHubPage_Default",
    name: "ResourceHub Page - Default",
    propertiesType: typeof(ResourceHubPageTemplateProperties),
    customViewName: "~/Features/ResourceHub/ResourceHubPage_Default.cshtml",
    ContentTypeNames = new[] { ResourceHubPage.CONTENT_TYPE_NAME },
    Description = "",
    IconClass = ""
)]

namespace Kentico.Community.Portal.Web.Features.ResourceHub;

public class ResourceHubPageTemplateProperties : IPageTemplateProperties { }
