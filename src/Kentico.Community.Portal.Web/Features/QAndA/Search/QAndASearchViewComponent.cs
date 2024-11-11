using System.Collections.Immutable;
using EnumsNET;
using Kentico.Community.Portal.Web.Components.ViewComponents.Pagination;
using Kentico.Community.Portal.Web.Features.Members.Badges;
using Kentico.Community.Portal.Web.Infrastructure.Search;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.QAndA.Search;

public class QAndASearchViewComponent(
    QAndASearchService searchService,
    MemberBadgeService memberBadgeService,
    IMediator mediator) : ViewComponent
{
    private readonly QAndASearchService searchService = searchService;
    private readonly MemberBadgeService memberBadgeService = memberBadgeService;
    private readonly IMediator mediator = mediator;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var request = new QAndASearchRequest(HttpContext.Request);

        var searchResult = searchService.SearchQAndA(request);
        var viewModels = searchResult.Hits.Select(QAndAPostViewModel.GetModel).ToList();
        var taxonomies = await mediator.Send(new QAndATaxonomiesQuery());

        viewModels = await memberBadgeService.AddSelectedBadgesToQAndA(viewModels);

        var vm = new QAndASearchViewModel(request, searchResult, viewModels, taxonomies);

        return View("~/Features/QAndA/Search/QAndASearch.cshtml", vm);
    }
}

public class QAndASearchViewModel : IPagedViewModel
{
    public string Query { get; } = "";
    public string SortBy { get; } = "";
    public IReadOnlyList<QAndAPostViewModel> Questions { get; }
    public ImmutableList<FacetOption> DiscussionTypes { get; }
    public ImmutableList<FacetOption> DXTopics { get; }
    public ImmutableList<FacetOption> DiscussionStates { get; }
    public DiscussionStates DiscussionState { get; }
    public int TotalAppliedFilters { get; }
    public int TotalPages { get; set; }
    [HiddenInput]
    public int Page { get; set; }

    public Dictionary<string, string?> GetRouteData(int page) =>
        new()
        {
            { "query", Query },
            { "page", page.ToString() },
            { "sortBy", SortBy }
        };

    public QAndASearchViewModel(QAndASearchRequest request, QAndASearchResult result, List<QAndAPostViewModel> viewModels, QAndATaxonomiesQueryResponse taxonomies)
    {
        Questions = viewModels;
        Page = request.PageNumber;
        SortBy = request.SortBy;
        Query = request.SearchText;
        TotalPages = result.TotalPages;
        DXTopics = [.. taxonomies.DXTopics
            .Select(x => new FacetOption()
            {
                Label = x.DisplayName,
                Value = x.NormalizedName,
                Count = result
                    .DXTopics
                    .FirstOrDefault(y => y.Label.Equals(x.NormalizedName, StringComparison.InvariantCultureIgnoreCase))
                    ?.Value ?? 0,
                IsSelected = request
                    .DXTopics
                    .Contains(x.NormalizedName, StringComparer.OrdinalIgnoreCase)
            })
            .Where(x => x.Count != 0)
            .OrderBy(f => f.Label)];
        DiscussionTypes = [.. taxonomies.Types
            .Select(x => new FacetOption()
            {
                Label = x.DisplayName,
                Value = x.NormalizedName,
                Count = result
                    .DiscussionTypes
                    .FirstOrDefault(y => y.Label.Equals(x.NormalizedName, StringComparison.InvariantCultureIgnoreCase))
                    ?.Value ?? 0,
                IsSelected = request
                    .DiscussionTypes
                    .Contains(x.NormalizedName, StringComparer.OrdinalIgnoreCase)
            })
            .Where(x => x.Count != 0)
            .OrderBy(f => f.Label)];
        DiscussionStates = [..Enums.GetMembers<DiscussionStates>()
            .Select(m => new FacetOption()
            {
                Label = m.AsString(EnumFormat.Description) ?? "",
                Value = (m.AsString(EnumFormat.Name) ?? "").ToLowerInvariant(),
                Count = result
                    .DiscussionStates
                    .FirstOrDefault(y => y.Label.Equals(m.AsString(EnumFormat.Name), StringComparison.InvariantCultureIgnoreCase))
                    ?.Value ?? 0,
                IsSelected = request.DiscussionStates.Contains(m.AsString(EnumFormat.Name) ?? "", StringComparer.OrdinalIgnoreCase)
            })
            .Where(x => x.Count != 0)
            .OrderBy(f => f.Label)];
        TotalAppliedFilters = DXTopics.Count(t => t.IsSelected) + DiscussionTypes.Count(t => t.IsSelected);
    }
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
        HasAcceptedResponse = Enums.TryParse<DiscussionStates>(result.DiscussionState, true, out var state)
            && state == DiscussionStates.HasAcceptedAnswer,
        Author = new(result.AuthorMemberID, result.AuthorFullName, result.AuthorUsername, result.AuthorAttributes),
        LinkPath = result.Url,
        ID = result.ID
    };
}

