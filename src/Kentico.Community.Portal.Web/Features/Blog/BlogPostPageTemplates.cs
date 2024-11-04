using Kentico.Community.Portal.Web.Features.Blog;
using Kentico.Community.Portal.Web.Features.Members;
using Kentico.Community.Portal.Web.Features.QAndA;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterPageTemplate(
    identifier: "KenticoCommunity.BlogPostPage_Default",
    name: "Blog Post Page - Default",
    propertiesType: typeof(BlogPostPageTemplateProperties),
    customViewName: "~/Features/Blog/BlogPostPage_Default.cshtml",
    ContentTypeNames = [BlogPostPage.CONTENT_TYPE_NAME],
    Description = "Displays the blog post page content from the Blog Post Content fields",
    IconClass = ""
)]

[assembly: RegisterPageTemplate(
    identifier: "KenticoCommunity.BlogPostPage_Components",
    name: "Blog Post Page - Components",
    propertiesType: typeof(BlogPostPageTemplateProperties),
    customViewName: "~/Features/Blog/BlogPostPage_Components.cshtml",
    ContentTypeNames = [BlogPostPage.CONTENT_TYPE_NAME],
    Description = "Requires the blog post page to be built with Page Builder components",
    IconClass = ""
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: BlogPostPage.CONTENT_TYPE_NAME,
    controllerType: typeof(BlogPostPageTemplateController)
)]

namespace Kentico.Community.Portal.Web.Features.Blog;

public class BlogPostPageTemplateProperties : IPageTemplateProperties { }

public class BlogPostPageTemplateController(
    WebPageMetaService metaService,
    IMediator mediator,
    MarkdownRenderer renderer,
    IWebPageDataContextRetriever contextRetriever,
    IWebPageUrlRetriever urlRetriever) : Controller
{
    private readonly WebPageMetaService metaService = metaService;
    private readonly IMediator mediator = mediator;
    private readonly MarkdownRenderer renderer = renderer;
    private readonly IWebPageDataContextRetriever contextRetriever = contextRetriever;
    private readonly IWebPageUrlRetriever urlRetriever = urlRetriever;

    public async Task<ActionResult> Index()
    {
        if (!contextRetriever.TryRetrieve(out var data))
        {
            return NotFound();
        }

        var blogPage = await mediator.Send(new BlogPostPageQuery(data.WebPage));
        var post = blogPage.BlogPostPageBlogPostContent.First();
        var author = await GetAuthor(post);
        var contentHTML = post.IsContentTypeMarkdown()
            ? renderer.RenderUnsafe(post.BlogPostContentContentMarkdown)
            : new(post.BlogPostContentContentHTML);

        var vm = new BlogPostDetailViewModel(new()
        {
            Photo = author.ToImageViewModel(),
            BiographyHTML = new(author.AuthorContentBiographyHTML),
            LinkProfilePath = GetAuthorMemberProfilePath(author),
            Name = author.FullName,
            Title = "" // TODO collect from Member
        })
        {
            Teaser = post.ToImageViewModel(),
            Title = post.ListableItemTitle,
            Date = post.BlogPostContentPublishedDate,
            UnsanitizedContentHTML = contentHTML,
            AbsoluteURL = Request.Path.Value!.RelativePathToAbsoluteURL(Request),
            DiscussionLinkPath = await GetDiscussionLinkPath(blogPage, data.WebPage.WebsiteChannelName)
        };

        metaService.SetMeta(blogPage.GetWebpageMeta());

        return new TemplateResult(vm);
    }

    private async Task<AuthorContent> GetAuthor(BlogPostContent post)
    {
        var author = post.BlogPostContentAuthor.FirstOrDefault();

        if (author is not null)
        {
            return author;
        }

        var resp = await mediator.Send(new AuthorContentQuery(AuthorContent.KENTICO_AUTHOR_CODE_NAME));

        if (resp.Author is null)
        {
            throw new Exception($"Missing Author [{AuthorContent.KENTICO_AUTHOR_CODE_NAME}] which is required to display BlogPosts");
        }

        return resp.Author;
    }

    private Maybe<string> GetAuthorMemberProfilePath(AuthorContent author)
    {
        if (author.AuthorContentMemberID <= 0)
        {
            return Maybe<string>.None;
        }

        string? path = Url.Action(nameof(MemberController.MemberDetail), "Member", new { memberID = author.AuthorContentMemberID });

        return string.IsNullOrWhiteSpace(path)
            ? Maybe<string>.None
            : path;
    }

    private async Task<string?> GetDiscussionLinkPath(BlogPostPage blogPostPage, string channelName)
    {
        if (!string.IsNullOrWhiteSpace(blogPostPage.BlogPostPageQAndADiscussionLinkPath))
        {
            return blogPostPage.BlogPostPageQAndADiscussionLinkPath;
        }

        if (blogPostPage.BlogPostPageQAndADiscussionPage.FirstOrDefault() is not WebPageRelatedItem relatedItem)
        {
            return null;
        }

        var questionPage = await mediator.Send(new QAndAQuestionPageByGUIDQuery(relatedItem.WebPageGuid, channelName));

        var pageUrl = await urlRetriever.Retrieve(questionPage);

        return pageUrl.RelativePath;
    }
}

public class BlogPostDetailViewModel(AuthorViewModel author)
{
    public Maybe<ImageViewModel> Teaser { get; init; }
    public string Title { get; init; } = "";
    public DateTime Date { get; init; }
    public HtmlString UnsanitizedContentHTML { get; init; } = HtmlString.Empty;
    public AuthorViewModel Author { get; init; } = author;
    public string AbsoluteURL { get; init; } = "";
    public string? DiscussionLinkPath { get; init; }
}

public class AuthorViewModel
{
    public string Name { get; init; } = "";
    public Maybe<string> Title { get; init; }
    public Maybe<ImageViewModel> Photo { get; init; }
    public Maybe<string> LinkProfilePath { get; init; }
    public HtmlString BiographyHTML { get; init; } = HtmlString.Empty;
}
