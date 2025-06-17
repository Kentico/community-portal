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
    IContentRetriever contentRetriever,
    IWebPageUrlRetriever urlRetriever) : Controller
{
    private readonly WebPageMetaService metaService = metaService;
    private readonly IMediator mediator = mediator;
    private readonly IContentRetriever contentRetriever = contentRetriever;
    private readonly IWebPageUrlRetriever urlRetriever = urlRetriever;

    public async Task<ActionResult> Index()
    {
        var blogPage = await contentRetriever.RetrieveCurrentPage<BlogPostPage>(new RetrieveCurrentPageParameters() { LinkedItemsMaxLevel = BlogPostPage.FullQueryDepth });
        if (blogPage is null)
        {
            return NotFound();
        }

        var author = await GetAuthor(blogPage);
        string absoluteURL = Request.Path.Value!.RelativePathToAbsoluteURL(Request);
        var discussionURL = await blogPage.BlogPostPageQAndAQuestionPages
            .TryFirst()
            .Map(p => urlRetriever.Retrieve(p))
            .Map(u => u.RelativePath);
        int commentCount = await GetDiscussionCommentCount(blogPage);
        var vm = new BlogPostPageViewModel(blogPage, author, discussionURL, absoluteURL, commentCount);

        metaService.SetMeta(blogPage);

        return new TemplateResult(vm);
    }

    private async Task<AuthorViewModel> GetAuthor(BlogPostPage post)
    {
        var author = post.BlogPostPageAuthorContent.FirstOrDefault();
        if (author is not null)
        {
            return new(author, Url);
        }
        var authors = await contentRetriever.RetrieveContent<AuthorContent>(
            new RetrieveContentParameters(),
            p => p.Where(w => w.WhereEquals(nameof(AuthorContent.AuthorContentCodeName), AuthorContent.KENTICO_AUTHOR_CODE_NAME)),
            new RetrievalCacheSettings($"{nameof(AuthorContent.AuthorContentCodeName)}|{AuthorContent.KENTICO_AUTHOR_CODE_NAME}"));

        if (authors.FirstOrDefault() is not AuthorContent kenticoAuthor)
        {
            throw new Exception($"Missing Author [{AuthorContent.KENTICO_AUTHOR_CODE_NAME}] which is required to display BlogPosts");
        }

        return new(kenticoAuthor, Url);
    }

    private async Task<int> GetDiscussionCommentCount(BlogPostPage blogPostPage)
    {
        if (blogPostPage.BlogPostPageQAndAQuestionPages.FirstOrDefault() is not QAndAQuestionPage question)
        {
            return 0;
        }

        return await mediator.Send(new QAndAAnswerCountQuery(question.SystemFields.WebPageItemGUID));
    }
}

public class BlogPostPageViewModel
{
    public string Title { get; } = "";
    public DateTime PublishedDate { get; }
    public AuthorViewModel Author { get; }
    public string AbsoluteURL { get; } = "";
    public Maybe<string> DiscussionLinkPath { get; }
    public int DiscussionCommentsCount { get; }

    public BlogPostPageViewModel(BlogPostPage page, AuthorViewModel author, Maybe<string> discussionURL, string absoluteURL, int commentCount)
    {
        Title = page.BasicItemTitle;
        PublishedDate = page.BlogPostPagePublishedDate;
        Author = author;
        DiscussionLinkPath = discussionURL;
        AbsoluteURL = absoluteURL;
        DiscussionCommentsCount = commentCount;
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
