using System.Collections.Immutable;
using EnumsNET;
using Kentico.Community.Portal.Core.Modules;
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
    public IReadOnlyList<FacetOption> DiscussionTypes { get; }
    public int DiscussionTypesSelected { get; }
    public IReadOnlyList<FacetGroup> DXTopics { get; }
    public int DXTopicsSelected { get; }
    public IReadOnlyList<FacetOption> DiscussionStates { get; }
    public int DiscussionStatesSelected { get; }
    public DiscussionStates DiscussionState { get; }
    public int TotalAppliedFilters { get; }
    public int TotalPages { get; set; }
    [HiddenInput]
    public int Page { get; set; }

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
        if (DiscussionTypes.Any(t => t.IsSelected))
        {
            routeData["discussionTypes"] = string.Join("&discussionTypes=", DiscussionTypes.Where(t => t.IsSelected).Select(t => t.Value)).Trim('&');
        }
        if (DXTopics.Any(g => g.Count > 0))
        {
            routeData["dxTopics"] = string.Join("&dxTopics=", DXTopics.SelectMany(t => t.Facets).Where(f => f.IsSelected).Select(f => f.Value)).Trim('&');
        }

        return routeData;
    }

    public QAndASearchViewModel(QAndASearchRequest request, QAndASearchResult result, List<QAndAPostViewModel> viewModels, QAndATaxonomiesQueryResponse taxonomies)
    {
        Questions = viewModels;
        Page = request.PageNumber;
        SortBy = request.SortBy;
        Query = request.SearchText;
        TotalPages = result.TotalPages;
        DXTopics = BuildGroups(taxonomies.DXTopicsHierarchy, result, request).ToList();
        DXTopicsSelected = DXTopics.Sum(g => g.Count);
        DiscussionTypes = [.. taxonomies.Types
            .Select(x => new FacetOption()
            {
                Label = x.DisplayName,
                Value = x.NormalizedName,
                Count = (int)Math.Round(result
                    .DiscussionTypes
                    .FirstOrDefault(y => y.Label.Equals(x.NormalizedName, StringComparison.InvariantCultureIgnoreCase))
                    ?.Value ?? 0),
                IsSelected = request
                    .DiscussionTypes
                    .Contains(x.NormalizedName, StringComparer.OrdinalIgnoreCase)
            })
            .OrderBy(f => f.Label)];
        DiscussionTypesSelected = DiscussionTypes.Count(t => t.IsSelected);
        DiscussionStates = [.. Enums.GetMembers<DiscussionStates>()
            .Select(m => new FacetOption()
            {
                Label = m.AsString(EnumFormat.Description) ?? "",
                Value = (m.AsString(EnumFormat.Name) ?? "").ToLowerInvariant(),
                Count = (int)Math.Round(result
                    .DiscussionStates
                    .FirstOrDefault(y => y.Label.Equals(m.AsString(EnumFormat.Name), StringComparison.InvariantCultureIgnoreCase))
                    ?.Value ?? 0),
                IsSelected = request.DiscussionStates.Contains(m.AsString(EnumFormat.Name) ?? "", StringComparer.OrdinalIgnoreCase)
            })
            .OrderBy(f => f.Label)];
        DiscussionStatesSelected = DiscussionStates.Count(t => t.IsSelected);
        TotalAppliedFilters = DXTopics.Sum(t => t.Count) + DiscussionTypes.Count(t => t.IsSelected);

        IEnumerable<FacetGroup> BuildGroups(IReadOnlyList<TaxonomyTag> parentTags, QAndASearchResult results, QAndASearchRequest request)
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

