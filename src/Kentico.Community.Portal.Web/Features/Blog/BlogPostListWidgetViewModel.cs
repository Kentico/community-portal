using Kentico.Community.Portal.Web.Components.ViewComponents.Pagination;
using Kentico.Community.Portal.Web.Components.Widgets.BlogPostList;
using Kentico.Community.Portal.Web.Infrastructure.Search;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.Blog;

public class BlogPostListWidgetViewModel : IPagedViewModel
{
    public string? Heading { get; } = "";
    public IReadOnlyList<BlogPostViewModel> BlogPosts { get; set; } = [];
    public ItemLayout Layout { get; set; } = ItemLayout.Minimal;
    public string? Query { get; set; } = "";
    public string SortBy { get; set; } = "";
    [HiddenInput]
    public string BlogType { get; set; } = "";
    [HiddenInput]
    public int Page { get; set; } = 0;
    public List<FacetOption> BlogTypes { get; set; } = [];
    public int TotalPages { get; set; } = 0;

    public BlogPostListWidgetViewModel(BlogPostListWidgetProperties props, IEnumerable<BlogPostViewModel> posts)
    {
        Heading = string.IsNullOrWhiteSpace(props.Heading) ? null : props.Heading;
        BlogPosts = posts.ToList();
        Layout = props.ItemLayoutSourceParsed;
    }

    public BlogPostListWidgetViewModel() { }

    public Dictionary<string, string?> GetRouteData(int page) =>
        new()
        {
            { "query", Query },
            { "page", page.ToString() },
            { "sortBy", SortBy },
            { "blogType", BlogType }
        };
}

