using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Rendering;
using Kentico.Content.Web.Mvc;
using MediatR;

namespace Kentico.Community.Portal.Web.Features.Blog.Components;

public class BlogPostDetailViewComponent : ViewComponent
{
    private readonly AssetItemService assetService;
    private readonly WebPageMetaService metaService;
    private readonly IMediator mediator;
    private readonly MarkdownRenderer renderer;

    public BlogPostDetailViewComponent(
        AssetItemService assetService,
        WebPageMetaService metaService,
        IMediator mediator,
        MarkdownRenderer renderer)
    {
        this.assetService = assetService;
        this.metaService = metaService;
        this.mediator = mediator;
        this.renderer = renderer;
    }

    public async Task<IViewComponentResult> InvokeAsync(IRoutedWebPage page)
    {

        var resp = await mediator.Send(new BlogPostPageQuery(page));

        var blog = resp.Page;

        var author = await GetAuthor(blog);
        var authorImage = await assetService.RetrieveMediaFileImage(author.AuthorContentPhotoMediaFileImage.FirstOrDefault());

        var teaser = await assetService.RetrieveMediaFileImage(blog.BlogPostPageTeaserMediaFileImage.FirstOrDefault());
        var contentHTML = blog.IsContentTypeMarkdown()
            ? renderer.Render(blog.BlogPostPageContentMarkdown)
            : new(blog.BlogPostPageContentHTML);

        var vm = new BlogPostDetailViewModel()
        {
            Title = blog.BlogPostPageTitle,
            Date = blog.BlogPostPageDate,
            HTMLSanitizedContentHTML = contentHTML,
            Teaser = teaser,
            Author = new()
            {
                Avatar = authorImage,
                BiographyHTML = new(author.AuthorContentBiographyHTML),
                LinkProfilePath = null, // TODO connect to Member
                Name = author.FullName,
                Title = "" // TODO collect from Member
            },
            AbsoluteURL = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}"
        };

        var meta = new Meta(blog.BlogPostPageTitle, blog.BlogPostPageShortDescription);

        metaService.SetMeta(meta);

        return View("~/Features/Blog/Components/BlogPostDetail.cshtml", vm);
    }

    private async Task<AuthorContent> GetAuthor(BlogPostPage post)
    {
        var author = post.BlogPostPageAuthor.FirstOrDefault();

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
}

public class BlogPostDetailViewModel
{
    public ImageAssetViewModel Teaser { get; init; }
    public string Title { get; init; }
    public DateTime Date { get; init; }
    public HtmlSanitizedHtmlString HTMLSanitizedContentHTML { get; init; }
    public AuthorViewModel Author { get; init; }
    public string AbsoluteURL { get; init; }
}

public class AuthorViewModel
{
    public string Name { get; init; }
    public string? Title { get; init; }
    public ImageAssetViewModel? Avatar { get; init; }
    public string? LinkProfilePath { get; init; }
    public HtmlString BiographyHTML { get; init; }
}

