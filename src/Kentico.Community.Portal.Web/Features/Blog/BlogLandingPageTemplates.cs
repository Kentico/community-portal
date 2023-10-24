using Kentico.Community.Portal.Web.Features.Blog;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using Kentico.Community.Portal.Core.Content;

[assembly: RegisterPageTemplate(
    identifier: "KenticoCommunity.BlogLandingPage_Default",
    name: "Blog Page - Default",
    propertiesType: typeof(BlogLandingPageTemplateProperties),
    customViewName: "~/Features/Blog/BlogLandingPage_Default.cshtml",
    ContentTypeNames = new[] { BlogLandingPage.CONTENT_TYPE_NAME },
    Description = "",
    IconClass = ""
)]

namespace Kentico.Community.Portal.Web.Features.Blog;

public class BlogLandingPageTemplateProperties : IPageTemplateProperties
{

}
