using CMS.Websites.Routing;
using Kentico.Community.Portal.Web.Features.Blog;
using Kentico.Community.Portal.Web.Features.Members;
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
    ContentTypeNames = new[] { BlogPostPage.CONTENT_TYPE_NAME },
    Description = "",
    IconClass = ""
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: BlogPostPage.CONTENT_TYPE_NAME,
    controllerType: typeof(BlogPostPageTemplateController)
)]

namespace Kentico.Community.Portal.Web.Features.Blog;

public class BlogPostPageTemplateProperties : IPageTemplateProperties { }

public class BlogPostPageTemplateController : Controller
{
    private readonly AssetItemService assetService;
    private readonly WebPageMetaService metaService;
    private readonly IMediator mediator;
    private readonly MarkdownRenderer renderer;
    private readonly IWebsiteChannelContext channelContext;
    private readonly IWebPageDataContextRetriever contextRetriever;

    public BlogPostPageTemplateController(
        AssetItemService assetService,
        WebPageMetaService metaService,
        IMediator mediator,
        MarkdownRenderer renderer,
        IWebsiteChannelContext channelContext,
        IWebPageDataContextRetriever contextRetriever)
    {
        this.assetService = assetService;
        this.metaService = metaService;
        this.mediator = mediator;
        this.renderer = renderer;
        this.channelContext = channelContext;
        this.contextRetriever = contextRetriever;
    }

    public async Task<ActionResult> Index()
    {
        if (!contextRetriever.TryRetrieve(out var data))
        {
            return NotFound();
        }

        var blogPage = await mediator.Send(new BlogPostPageQuery(data.WebPage, channelContext.WebsiteChannelName));
        var post = blogPage.BlogPostPageBlogPostContent.First();

        var author = await GetAuthor(post);
        var authorImage = await assetService.RetrieveMediaFileImage(author.AuthorContentPhotoMediaFileImage.FirstOrDefault());

        var teaser = await assetService.RetrieveMediaFileImage(post.BlogPostContentTeaserMediaFileImage.FirstOrDefault());
        var contentHTML = post.IsContentTypeMarkdown()
            ? renderer.RenderUnsafe(post.BlogPostContentContentMarkdown)
            : new(post.BlogPostContentContentHTML);

        var vm = new BlogPostDetailViewModel()
        {
            Title = post.BlogPostContentTitle,
            Date = post.BlogPostContentPublishedDate,
            UnsanitizedContentHTML = contentHTML,
            Teaser = teaser,
            Author = new()
            {
                Avatar = authorImage,
                BiographyHTML = new(author.AuthorContentBiographyHTML),
                LinkProfilePath = GetAuthorMemberProfilePath(author),
                Name = author.FullName,
                Title = "" // TODO collect from Member
            },
            AbsoluteURL = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}",
            DiscussionLinkPath = string.IsNullOrWhiteSpace(blogPage.BlogPostPageQAndADiscussionLinkPath)
                ? null
                : blogPage.BlogPostPageQAndADiscussionLinkPath
        };

        var meta = new Meta(blogPage.BlogPostPageTitle, blogPage.BlogPostPageShortDescription);

        metaService.SetMeta(meta);

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

        return Url.Action(nameof(MemberController.MemberDetail), "Member", new { memberID = author.AuthorContentMemberID });
    }
}

public class BlogPostDetailViewModel
{
    public ImageAssetViewModel Teaser { get; init; }
    public string Title { get; init; }
    public DateTime Date { get; init; }
    public HtmlString UnsanitizedContentHTML { get; init; }
    public AuthorViewModel Author { get; init; }
    public string AbsoluteURL { get; init; }
    public string? DiscussionLinkPath { get; init; }
}

public class AuthorViewModel
{
    public string Name { get; init; }
    public string? Title { get; init; }
    public ImageAssetViewModel? Avatar { get; init; }
    public Maybe<string> LinkProfilePath { get; init; }
    public HtmlString BiographyHTML { get; init; }
}
