using System.Collections.Immutable;
using EnumsNET;
using Kentico.Community.Portal.Web.Components.ViewComponents.Pagination;
using Kentico.Community.Portal.Web.Features.QAndA.Search;
using Kentico.Community.Portal.Web.Infrastructure.Search;
using Kentico.Community.Portal.Web.Rendering;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.Blog.Search;

public class BlogSearchViewComponent(IMediator mediator, BlogSearchService searchService) : ViewComponent
{
    private readonly IMediator mediator = mediator;
    private readonly BlogSearchService searchService = searchService;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var request = new BlogSearchRequest(HttpContext.Request);
        var taxonomies = await mediator.Send(new BlogPostTaxonomiesQuery());
        var searchResult = searchService.SearchBlog(request);
        var model = new BlogSearchViewModel(request, searchResult, taxonomies);

        return View("~/Features/Blog/Search/BlogSearch.cshtml", model);
    }
}

public class BlogSearchViewModel : IPagedViewModel
{
    public IReadOnlyList<BlogPostSearchResultViewModel> BlogPosts { get; }
    public string? Query { get; }
    public string SortBy { get; }
    public ImmutableList<FacetOption> DXTopics { get; }
    public ImmutableList<FacetOption> BlogTypes { get; }
    public int TotalAppliedFilters { get; }
    [HiddenInput]
    public int Page { get; set; } = 0;
    public int TotalPages { get; set; } = 0;

    public Dictionary<string, string?> GetRouteData(int page) =>
        new()
        {
            { "query", Query },
            { "page", page.ToString() },
            { "sortBy", SortBy },
            { "blogTypes", string.Join(",", BlogTypes) }
        };

    public BlogSearchViewModel(BlogSearchRequest request, BlogSearchResultsViewModel result, BlogPostTaxonomiesQueryResponse taxonomies)
    {
        BlogPosts = result
            .Hits
            .Select(result => new BlogPostSearchResultViewModel(result))
            .ToList();

        Page = request.PageNumber;
        Query = request.SearchText;
        SortBy = request.SortBy;
        BlogTypes = [.. taxonomies.Types
            .Select(x => new FacetOption()
            {
                Label = x.DisplayName,
                Value = x.NormalizedName,
                Count = result
                    .BlogTypes
                    .FirstOrDefault(y => y.Label.Equals(x.NormalizedName, StringComparison.InvariantCultureIgnoreCase))?.Value ?? 0,
                IsSelected = request
                    .BlogTypes
                    .Contains(x.NormalizedName, StringComparer.OrdinalIgnoreCase)
            })
            .Where(x => x.Count != 0)
            .OrderBy(f => f.Label)];
        DXTopics = [.. taxonomies.DXTopics
            .Select(x => new FacetOption()
            {
                Label = x.DisplayName,
                Value = x.NormalizedName,
                Count = result
                    .BlogDXTopics
                    .FirstOrDefault(y => y.Label.Equals(x.NormalizedName, StringComparison.InvariantCultureIgnoreCase))?.Value ?? 0,
                IsSelected = request
                    .DXTopics
                    .Contains(x.NormalizedName, StringComparer.OrdinalIgnoreCase)
            })
            .Where(x => x.Count != 0)
            .OrderBy(f => f.Label)];
        TotalAppliedFilters = BlogTypes.Count(t => t.IsSelected) + DXTopics.Count(t => t.IsSelected);
        TotalPages = result?.TotalPages ?? 0;
    }
}

public class BlogPostSearchResultViewModel
{
    public string Title { get; } = "";
    public DateTime Date { get; }
    public Maybe<ImageViewModel> TeaserImage { get; }
    public BlogPostAuthorViewModel Author { get; }
    public string ShortDescription { get; } = "";
    public string LinkPath { get; } = "";
    public string BlogType { get; } = "";
    public IEnumerable<string> DXTopics { get; } = [];

    public BlogPostSearchResultViewModel(BlogSearchIndexModel model)
    {
        Author = new()
        {
            ID = model.AuthorMemberID,
            Name = model.AuthorName,
            Photo = Maybe.From(model.AuthorAvatarImage!).Map(i => i.ToImageViewModel()),
        };

        Title = model.Title;
        Date = model.PublishedDate;
        LinkPath = model.Url;
        ShortDescription = model.ShortDescription;
        TeaserImage = Maybe.From(model.TeaserImage!).Map(i => i.ToImageViewModel());
        BlogType = model.BlogType;
        DXTopics = model.DXTopics;
    }
}
