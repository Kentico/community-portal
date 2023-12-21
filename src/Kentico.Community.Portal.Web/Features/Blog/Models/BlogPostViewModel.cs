using Kentico.Community.Portal.Web.Rendering;

namespace Kentico.Community.Portal.Web.Features.Blog.Models;

public class BlogPostViewModel
{
    public BlogPostViewModel(BlogPostAuthorViewModel author) => Author = author;

    public string Title { get; init; } = "";
    public DateTime Date { get; init; }
    public ImageAssetViewModel? TeaserImage { get; init; }
    public BlogPostAuthorViewModel Author { get; init; }
    public string ShortDescription { get; init; } = "";
    public string LinkPath { get; init; } = "";
    public string? Taxonomy { get; set; } = "";
}
