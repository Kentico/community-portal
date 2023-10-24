using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using Kentico.Community.Portal.Web.Features.Posts;
using Kentico.Community.Portal.Core.Content;

[assembly: RegisterPageTemplate(
    identifier: "KenticoCommunity.BlogPostPage_Default",
    name: "Blog Post Page - Default",
    propertiesType: typeof(BlogPostPageTemplateProperties),
    customViewName: "~/Features/Blog/BlogPostPage_Default.cshtml",
    ContentTypeNames = new[] { BlogPostPage.CONTENT_TYPE_NAME },
    Description = "",
    IconClass = ""
)]

namespace Kentico.Community.Portal.Web.Features.Posts;

public class BlogPostPageTemplateProperties : IPageTemplateProperties { }
