using Kentico.Community.Portal.Web.Rendering;

namespace Kentico.Community.Portal.Web.Features.Blog;

public class BlogPostViewModel(BlogPostAuthorViewModel author)
{
    public string Title { get; init; } = "";
    public DateTime Date { get; init; }
    public Maybe<ImageViewModel> TeaserImage { get; init; }
    public BlogPostAuthorViewModel Author { get; init; } = author;
    public string ShortDescription { get; init; } = "";
    public string LinkPath { get; init; } = "";
    public string? Taxonomy { get; set; } = "";
}
