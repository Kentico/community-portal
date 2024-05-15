using CMS.ContentEngine;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Components.ViewComponents.Pagination;
using Kentico.Community.Portal.Web.Features.QAndA.Search;
using Kentico.Community.Portal.Web.Infrastructure.Search;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public class QAndASearchViewComponent(QAndASearchService searchService, ITaxonomyRetriever taxonomyRetriever) : ViewComponent
{
    private readonly QAndASearchService searchService = searchService;
    private readonly ITaxonomyRetriever taxonomyRetriever = taxonomyRetriever;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var request = new QAndASearchRequest(HttpContext.Request);

        var searchResult = searchService.SearchQAndA(request);
        var chosenFacets = request.DiscussionType.ToLower().Split(";", StringSplitOptions.RemoveEmptyEntries)?.ToList() ?? [];
        var viewModels = searchResult.Hits.Select(QAndAPostViewModel.GetModel).ToList();

        var taxonomy = await taxonomyRetriever.RetrieveTaxonomy(SystemTaxonomies.QAndADiscussionTypeTaxonomy.CodeName, PortalWebSiteChannel.DEFAULT_LANGUAGE);

        var vm = new QAndASearchViewModel
        {
            Questions = viewModels,
            Page = request.PageNumber,
            SortBy = request.SortBy,
            Query = request.SearchText,
            TotalPages = searchResult.TotalPages,
            OnlyAcceptedResponses = request.OnlyAcceptedResponses,
            DiscussionType = request.DiscussionType,
            DiscussionTypes = [.. taxonomy.Tags
                .Select(x => new FacetOption()
                {
                    Label = x.Title,
                    Value = searchResult?.Facets?.FirstOrDefault(y => y.Label.Equals(x.Title, StringComparison.InvariantCultureIgnoreCase))?.Value ?? 0,
                    IsSelected = chosenFacets.Contains(x.Title, StringComparer.OrdinalIgnoreCase)
                })
                .Where(x => x.Value != 0)
                .OrderBy(f => f.Label)],
        };

        return View("~/Features/QAndA/Components/Search/QAndASearch.cshtml", vm);
    }
}

public class QAndASearchViewModel : IPagedViewModel
{
    public IReadOnlyList<QAndAPostViewModel> Questions { get; set; } = [];

    public string? Query { get; set; } = "";
    [HiddenInput]
    public int Page { get; set; }
    public string SortBy { get; set; } = "";
    [HiddenInput]
    public string DiscussionType { get; set; } = "";
    public List<FacetOption> DiscussionTypes { get; set; } = [];
    public bool OnlyAcceptedResponses { get; set; } = false;
    public int TotalPages { get; set; }

    public Dictionary<string, string?> GetRouteData(int page) =>
        new()
        {
            { "query", Query },
            { "page", page.ToString() },
            { "sortBy", SortBy }
        };
}

public class QAndAPostViewModel
{
    public int ID { get; set; }
    public string Title { get; set; } = "";
    public string LinkPath { get; set; } = "";
    public DateTime DateCreated { get; set; }
    public int ResponseCount { get; set; }
    public DateTime LatestResponseDate { get; set; }
    public bool HasAcceptedResponse { get; set; }
    public QAndAAuthorViewModel Author { get; set; } = new();

    public static QAndAPostViewModel GetModel(QAndASearchIndexModel result) => new()
    {
        Title = result.Title,
        DateCreated = result.PublishedDate,
        ResponseCount = result.ResponseCount,
        LatestResponseDate = result.LatestResponseDate,
        HasAcceptedResponse = result.HasAcceptedResponse,
        Author = new(result.AuthorMemberID, result.AuthorFullName, result.AuthorUsername, result.AuthorAttributes),
        LinkPath = result.Url,
        ID = result.ID
    };
}

