using System.Collections.Immutable;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Components.ViewComponents.Pagination;
using Kentico.Community.Portal.Web.Infrastructure.Search;
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
    public IReadOnlyList<FacetGroup> DXTopics { get; }
    public int DXTopicsSelectedCount { get; }
    public IReadOnlyList<FacetOption> BlogTypes { get; }
    public int BlogTypesSelected { get; }
    public int TotalAppliedFilters { get; }
    [HiddenInput]
    public int Page { get; set; } = 0;
    public int TotalPages { get; set; } = 0;

    public Dictionary<string, string?> GetRouteData(int page)
    {
        var routeData = new Dictionary<string, string?>
        {
            ["page"] = page.ToString()
        };

        if (!string.IsNullOrWhiteSpace(Query))
        {
            routeData["query"] = Query;
        }
        if (!string.IsNullOrWhiteSpace(SortBy))
        {
            routeData["sortBy"] = SortBy;
        }
        if (BlogTypes.Any(t => t.IsSelected))
        {
            routeData["blogTypes"] = string.Join("&blogTypes=", BlogTypes.Where(t => t.IsSelected).Select(t => t.Value)).Trim('&');
        }
        if (DXTopics.Any(g => g.Count > 0))
        {
            routeData["dxTopics"] = string.Join("&dxTopics=", DXTopics.SelectMany(t => t.Facets).Where(f => f.IsSelected).Select(f => f.Value)).Trim('&');
        }

        return routeData;
    }

    public BlogSearchViewModel(BlogSearchRequest request, BlogSearchResults result, BlogPostTaxonomiesQueryResponse taxonomies)
    {
        BlogPosts = [.. result
            .Hits
            .Select(result => new BlogPostSearchResultViewModel(result))];

        Page = request.PageNumber;
        Query = request.SearchText;
        SortBy = request.SortBy;
        BlogTypes = [.. taxonomies.Types
            .Select(x => new FacetOption()
            {
                Label = x.DisplayName,
                Value = x.NormalizedName,
                Count = (int)Math.Round(result
                    .BlogTypes
                    .FirstOrDefault(y => y.Label.Equals(x.NormalizedName, StringComparison.InvariantCultureIgnoreCase))
                    ?.Value ?? 0),
                IsSelected = request
                    .BlogTypes
                    .Contains(x.NormalizedName, StringComparer.OrdinalIgnoreCase)
            })
            .OrderBy(f => f.Label)];
        BlogTypesSelected = BlogTypes.Count(t => t.IsSelected);
        DXTopics = BuildGroups(taxonomies.DXTopicsHierarchy, result, request).ToList();
        DXTopicsSelectedCount = DXTopics.Sum(g => g.Count);
        TotalAppliedFilters = BlogTypes.Count(t => t.IsSelected) + DXTopics.Sum(t => t.Count);
        TotalPages = result?.TotalPages ?? 0;

        IEnumerable<FacetGroup> BuildGroups(IReadOnlyList<TaxonomyTag> parentTags, BlogSearchResults results, BlogSearchRequest request)
        {
            // First, create a group for selected topics
            var selectedTopics = parentTags
                .SelectMany(p => p.Children)
                .Where(c => request.DXTopics.Contains(c.NormalizedName, StringComparer.OrdinalIgnoreCase))
                .Select(x => new FacetOption()
                {
                    Label = x.DisplayName,
                    Value = x.NormalizedName,
                    Count = (int)Math.Round(result.DXTopics
                        .FirstOrDefault(t => t.Label.Equals(x.NormalizedName, StringComparison.InvariantCultureIgnoreCase))
                        ?.Value ?? 0),
                    IsSelected = true
                })
                .ToList();

            // Always yield the selected topics group, even if empty
            yield return new FacetGroup()
            {
                Label = "Selected Topics",
                Value = "selected-topics",
                Count = selectedTopics.Count,
                Facets = selectedTopics
            };

            // Then return the regular groups with unselected topics
            foreach (var parent in parentTags)
            {
                var unselectedTopics = parent.Children
                    .Where(c => !request.DXTopics.Contains(c.NormalizedName, StringComparer.OrdinalIgnoreCase))
                    .Select(x => new FacetOption()
                    {
                        Label = x.DisplayName,
                        Value = x.NormalizedName,
                        Count = (int)Math.Round(result.DXTopics
                            .FirstOrDefault(t => t.Label.Equals(x.NormalizedName, StringComparison.InvariantCultureIgnoreCase))
                            ?.Value ?? 0),
                        IsSelected = false
                    })
                    .ToList();

                if (unselectedTopics.Count > 0)
                {
                    yield return new FacetGroup()
                    {
                        Label = parent.DisplayName,
                        Value = parent.NormalizedName,
                        Count = 0, // Since selected topics are in their own group
                        Facets = unselectedTopics
                    };
                }
            }
        }
    }
}

public class BlogPostSearchResultViewModel
{
    public string ID { get; }
    public string Title { get; } = "";
    public DateTime Date { get; }
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

        ID = model.ID;
        Title = model.Title;
        Date = model.PublishedDate;
        LinkPath = model.Url;
        ShortDescription = model.ShortDescription;
        BlogType = model.BlogType;
        DXTopics = model.DXTopics.OrderByDescending(t => t, StringComparer.OrdinalIgnoreCase);
    }
}
