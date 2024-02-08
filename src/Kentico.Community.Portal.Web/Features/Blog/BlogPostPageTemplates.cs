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
    ContentTypeNames = [BlogPostPage.CONTENT_TYPE_NAME],
    Description = "",
    IconClass = ""
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: BlogPostPage.CONTENT_TYPE_NAME,
    controllerType: typeof(BlogPostPageTemplateController)
)]

namespace Kentico.Community.Portal.Web.Features.Blog;

public class BlogPostPageTemplateProperties : IPageTemplateProperties { }

public class BlogPostPageTemplateController(
    AssetItemService assetService,
    WebPageMetaService metaService,
    IMediator mediator,
    MarkdownRenderer renderer,
    IWebPageDataContextRetriever contextRetriever) : Controller
{
    private readonly AssetItemService assetService = assetService;
    private readonly WebPageMetaService metaService = metaService;
    private readonly IMediator mediator = mediator;
    private readonly MarkdownRenderer renderer = renderer;
    private readonly IWebPageDataContextRetriever contextRetriever = contextRetriever;

    public async Task<ActionResult> Index()
    {
        if (!contextRetriever.TryRetrieve(out var data))
        {
            return NotFound();
        }

        var blogPage = await mediator.Send(new BlogPostPageQuery(data.WebPage));
        var post = blogPage.BlogPostPageBlogPostContent.First();

        var author = await GetAuthor(post);
        var authorImage = await assetService.RetrieveMediaFileImage(author.AuthorContentPhotoMediaFileImage.FirstOrDefault());

        var teaser = await assetService.RetrieveMediaFileImage(post.BlogPostContentTeaserMediaFileImage.FirstOrDefault());
        var contentHTML = post.IsContentTypeMarkdown()
            ? renderer.RenderUnsafe(post.BlogPostContentContentMarkdown)
            : new(post.BlogPostContentContentHTML);

        var vm = new BlogPostDetailViewModel(teaser, new()
        {
            Avatar = authorImage,
            BiographyHTML = new(author.AuthorContentBiographyHTML),
            LinkProfilePath = GetAuthorMemberProfilePath(author),
            Name = author.FullName,
            Title = "" // TODO collect from Member
        })
        {
            Title = post.BlogPostContentTitle,
            Date = post.BlogPostContentPublishedDate,
            UnsanitizedContentHTML = contentHTML,
            AbsoluteURL = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}",
            DiscussionLinkPath = string.IsNullOrWhiteSpace(blogPage.BlogPostPageQAndADiscussionLinkPath)
                ? null
                : blogPage.BlogPostPageQAndADiscussionLinkPath
        };

        string metaTitle = string.IsNullOrWhiteSpace(blogPage.WebPageMetaTitle)
            ? post.BlogPostContentTitle
            : blogPage.WebPageMetaTitle;
        string metaDescription = string.IsNullOrWhiteSpace(blogPage.WebPageMetaDescription)
            ? post.BlogPostContentShortDescription
            : blogPage.WebPageMetaDescription;

        metaService.SetMeta(new(metaTitle, metaDescription)
        {
            CanonicalURL = blogPage.BlogPostPageCanonicalURL
        });

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
}

public class BlogPostDetailViewModel(ImageAssetViewModel? teaser, AuthorViewModel author)
{
    public ImageAssetViewModel? Teaser { get; init; } = teaser;
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
    public string? Title { get; init; }
    public ImageAssetViewModel? Avatar { get; init; }
    public Maybe<string> LinkProfilePath { get; init; }
    public HtmlString BiographyHTML { get; init; } = HtmlString.Empty;
}
