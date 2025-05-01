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
    IWebPageDataContextRetriever contextRetriever,
    IWebPageUrlRetriever urlRetriever) : Controller
{
    private readonly WebPageMetaService metaService = metaService;
    private readonly IMediator mediator = mediator;
    private readonly IWebPageDataContextRetriever contextRetriever = contextRetriever;
    private readonly IWebPageUrlRetriever urlRetriever = urlRetriever;

    public async Task<ActionResult> Index()
    {
        if (!contextRetriever.TryRetrieve(out var data))
        {
            return NotFound();
        }

        var blogPage = await mediator.Send(new BlogPostPageQuery(data.WebPage));
        var author = await GetAuthor(blogPage);
        string absoluteURL = Request.Path.Value!.RelativePathToAbsoluteURL(Request);
        var discussionURL = await GetDiscussionLinkPath(blogPage, data.WebPage.WebsiteChannelName);
        var vm = new BlogPostPageViewModel(blogPage, author, discussionURL, absoluteURL);

        metaService.SetMeta(blogPage.GetWebpageMeta());

        return new TemplateResult(vm);
    }

    private async Task<AuthorViewModel> GetAuthor(BlogPostPage post)
    {
        var author = post.BlogPostPageAuthorContent.FirstOrDefault();
        if (author is not null)
        {
            return new(author, Url);
        }

        var resp = await mediator.Send(new AuthorContentQuery(AuthorContent.KENTICO_AUTHOR_CODE_NAME));
        if (resp.Author is null)
        {
            throw new Exception($"Missing Author [{AuthorContent.KENTICO_AUTHOR_CODE_NAME}] which is required to display BlogPosts");
        }

        return new(resp.Author, Url);
    }

    private async Task<Maybe<string>> GetDiscussionLinkPath(BlogPostPage blogPostPage, string channelName)
    {
        if (blogPostPage.BlogPostPageQAndADiscussionPage.FirstOrDefault() is not WebPageRelatedItem relatedItem)
        {
            return Maybe<string>.None;
        }

        return await mediator.Send(new QAndAQuestionPageByGUIDQuery(relatedItem.WebPageGuid, channelName))
            .Map(p => urlRetriever.Retrieve(p))
            .Map(url => url.RelativePath);
    }
}

public class BlogPostPageViewModel
{
    public string Title { get; } = "";
    public DateTime PublishedDate { get; }
    public AuthorViewModel Author { get; }
    public string AbsoluteURL { get; } = "";
    public Maybe<string> DiscussionLinkPath { get; }

    public BlogPostPageViewModel(BlogPostPage page, AuthorViewModel author, Maybe<string> discussionURL, string absoluteURL)
    {
        Title = page.WebPageMetaTitle;
        PublishedDate = page.BlogPostPagePublishedDate;
        Author = author;
        DiscussionLinkPath = discussionURL;
        AbsoluteURL = absoluteURL;
    }
}

public class AuthorViewModel
{
    public string Name { get; init; } = "";
    public Maybe<string> Title { get; init; }
    public Maybe<ImageViewModel> Photo { get; init; }
    public Maybe<string> LinkProfilePath { get; init; }
    public HtmlString BiographyHTML { get; init; } = HtmlString.Empty;

    public AuthorViewModel(AuthorContent author, IUrlHelper urlHelper)
    {
        Name = author.FullName;
        Photo = author.AuthorContentPhotoImageContent
            .TryFirst()
            .Map(ImageViewModel.Create);
        LinkProfilePath = author.AuthorContentMemberID == 0
            ? Maybe<string>.None
            : urlHelper.Action(nameof(MemberController.MemberDetail), "Member", new { memberID = author.AuthorContentMemberID });
        BiographyHTML = new(author.AuthorContentBiographyHTML);
    }
}
