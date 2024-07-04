using Kentico.Community.Portal.Web.Features.Blog.Search;
using Kentico.Community.Portal.Web.Infrastructure.Search;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.Blog.Components;

public class BlogSearchViewComponent(IMediator mediator, BlogSearchService searchService) : ViewComponent
{
    private readonly IMediator mediator = mediator;
    private readonly BlogSearchService searchService = searchService;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var request = new BlogSearchRequest(HttpContext.Request);

        var result = await mediator.Send(new BlogPostTaxonomiesQuery());
        var taxonomies = result.Items;

        var searchResult = searchService.SearchBlog(request);
        var chosenFacets = request.BlogType.ToLower().Split(";", StringSplitOptions.RemoveEmptyEntries)?.ToList() ?? [];

        var model = new BlogPostListWidgetViewModel()
        {
            BlogPosts = BuildPostPageViewModels(searchResult?.Hits),
            Page = request.PageNumber,
            Query = request.SearchText,
            SortBy = request.SortBy,
            BlogType = request.BlogType,
            BlogTypes = [.. taxonomies
                .Select(x => new FacetOption()
                {
                    Label = x.DisplayName,
                    Value = searchResult?.Facets?.FirstOrDefault(y => y.Label.Equals(x.Value, StringComparison.InvariantCultureIgnoreCase))?.Value ?? 0,
                    IsSelected = chosenFacets.Contains(x.DisplayName, StringComparer.OrdinalIgnoreCase)
                })
                .Where(x => x.Value != 0)
                .OrderBy(f => f.Label)],
            TotalPages = searchResult?.TotalPages ?? 0
        };

        return View("~/Features/Blog/Components/BlogSearch.cshtml", model);
    }

    private static List<BlogPostViewModel> BuildPostPageViewModels(IEnumerable<BlogSearchIndexModel>? results)
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
                Taxonomy = result.BlogType
            });
        }

        return vms;
    }
}

