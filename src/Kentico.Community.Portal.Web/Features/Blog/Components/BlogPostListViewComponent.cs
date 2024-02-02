using Kentico.Community.Portal.Web.Infrastructure.Search;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.Blog.Components;

public class BlogPostListViewComponent : ViewComponent
{
    private readonly IMediator mediator;
    private readonly SearchService searchService;

    public BlogPostListViewComponent(IMediator mediator, SearchService searchService)
    {
        this.mediator = mediator;
        this.searchService = searchService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var request = new BlogSearchRequest(HttpContext.Request);

        var result = await mediator.Send(new BlogPostTaxonomiesQuery());
        var taxonomies = result.Items;

        var searchResult = searchService.SearchBlog(request);
        var chosenFacets = request.Facet.ToLower().Split(";", StringSplitOptions.RemoveEmptyEntries)?.ToList() ?? [];

        var model = new BlogPostListWidgetViewModel()
        {
            BlogPosts = BuildPostPageViewModels(searchResult?.Hits),
            Page = request.PageNumber,
            Query = request.SearchText,
            SortBy = request.SortBy,
            Facet = request.Facet,
            Facets = [.. taxonomies
                .Select(x => new FacetOption()
                {
                    Label = x.DisplayName,
                    Value = searchResult?.Facets?.FirstOrDefault(y => y.Label == x.DisplayName.ToLowerInvariant())?.Value ?? 0,
                    IsSelected = chosenFacets.Contains(x.DisplayName, StringComparer.OrdinalIgnoreCase)
                })
                .Where(x => x.Value != 0)
                .OrderBy(f => f.Label)],
            ChosenFacets = chosenFacets,
            TotalPages = searchResult?.TotalPages ?? 0
        };

        foreach (var facetOption in model.Facets)
        {
            facetOption.IsSelected = model.ChosenFacets.Contains(facetOption.Label.ToLower());
        }

        return View("~/Features/Blog/Components/BlogPostList.cshtml", model);
    }

    private static IReadOnlyList<BlogPostViewModel> BuildPostPageViewModels(IEnumerable<BlogSearchModel>? results)
    {
        if (results is null)
        {
            return [];
        }

        var vms = new List<BlogPostViewModel>();

        foreach (var result in results)
        {
            vms.Add(new BlogPostViewModel(new()
            {
                ID = result.AuthorMemberID,
                Name = result.AuthorName,
                Avatar = result.AuthorAvatarImage?.ToImageAsset(),
            })
            {
                Title = result.Title,
                Date = result.PublishedDate,
                LinkPath = result.Url,
                ShortDescription = result.ShortDescription,
                TeaserImage = result.TeaserImage?.ToImageAsset(),
                Taxonomy = result.Taxonomy
            });
        }

        return vms;
    }
}

