using Kentico.Community.Portal.Web.Components.Widgets.BlogPostList;
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
            BlogPosts = (searchResult?.Hits ?? []).Select(result => new BlogPostViewModel(new()
            {
                ID = result.AuthorMemberID,
                Name = result.AuthorName,
                Photo = Maybe.From(result.AuthorAvatarImage!).Map(i => i.ToImageViewModel()),
            })
            {
                Title = result.Title,
                Date = result.PublishedDate,
                LinkPath = result.Url,
                ShortDescription = result.ShortDescription,
                TeaserImage = Maybe.From(result.TeaserImage!).Map(i => i.ToImageViewModel()),
                Taxonomy = result.BlogType
            }).ToList(),
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
}

