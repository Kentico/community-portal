using System.Collections.Immutable;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Components.ViewComponents.Pagination;
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
    public IReadOnlyList<FacetGroup> DXTopics { get; }
    public int DXTopicsSelected { get; }
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
        DXTopicsSelected = DXTopics.Sum(g => g.Count);
        TotalAppliedFilters = BlogTypes.Count(t => t.IsSelected) + DXTopics.Sum(t => t.Count);
        TotalPages = result?.TotalPages ?? 0;

        IEnumerable<FacetGroup> BuildGroups(IReadOnlyList<TaxonomyTag> parentTags, BlogSearchResults results, BlogSearchRequest request)
        {
            foreach (var parent in parentTags)
            {
                yield return new FacetGroup()
                {
                    Label = parent.DisplayName,
                    Value = parent.NormalizedName,
                    Count = parent.Children.Count(c => request
                        .DXTopics
                        .Contains(c.NormalizedName, StringComparer.OrdinalIgnoreCase)),
                    Facets = [.. parent.Children
                        .Select(x => new FacetOption()
                        {
                            Label = x.DisplayName,
                            Value = x.NormalizedName,
                            Count = (int)Math.Round(result
                                .DXTopics
                                .FirstOrDefault(t => t.Label.Equals(x.NormalizedName, StringComparison.InvariantCultureIgnoreCase))?.Value ?? 0),
                            IsSelected = request
                                .DXTopics
                                .Contains(x.NormalizedName, StringComparer.OrdinalIgnoreCase)
                        })
                        .If(string.Equals(parent.NormalizedName, "Refreshes", StringComparison.OrdinalIgnoreCase), fs => fs.OrderByDescending(x => x.Label))],
                };
            }
        }
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
