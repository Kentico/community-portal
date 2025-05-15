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
        var taxonomies = await mediator.Send(new QAndATaxonomiesQuery());
        var discussions = searchResult.Hits.Select(h => new QAndADiscussionViewModel(h)).ToList();

        discussions = await memberBadgeService.AddSelectedBadgesToQAndA(discussions);

        var vm = new QAndASearchViewModel(request, searchResult, discussions, taxonomies);

        return View("~/Features/QAndA/Search/QAndASearch.cshtml", vm);
    }
}

public class QAndASearchViewModel : IPagedViewModel
{
    public string Query { get; } = "";
    public string SortBy { get; } = "";
    public IReadOnlyList<QAndADiscussionViewModel> Discussions { get; }
    public IReadOnlyList<FacetOption> DiscussionTypes { get; }
    public int DiscussionTypesSelected { get; }
    public IReadOnlyList<FacetGroup> DXTopics { get; }
    public int DXTopicsSelectedCount { get; }
    public IReadOnlyList<FacetOption> DiscussionStates { get; }
    public int DiscussionStatesSelected { get; }
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

    public QAndASearchViewModel(QAndASearchRequest request, QAndASearchResult result, List<QAndADiscussionViewModel> discussions, QAndATaxonomiesQueryResponse taxonomies)
    {
        Discussions = discussions;
        Page = request.PageNumber;
        SortBy = request.SortBy;
        Query = request.SearchText;
        TotalPages = result.TotalPages;

        DXTopics = [.. BuildGroups(taxonomies.DXTopicsHierarchy, result, request)];
        DXTopicsSelectedCount = DXTopics.TryFirst().Map(t => t.Facets.Count).GetValueOrDefault();

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
        TotalAppliedFilters = DXTopicsSelectedCount + DiscussionTypes.Count(t => t.IsSelected);

        IEnumerable<FacetGroup> BuildGroups(IReadOnlyList<TaxonomyTag> parentTags, QAndASearchResult results, QAndASearchRequest request)
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

public class QAndADiscussionViewModel
{
    public int ID { get; }
    public string Title { get; } = "";
    public string LinkPath { get; } = "";
    public DateTime DateCreated { get; }
    public int ResponseCount { get; }
    public DateTime LatestResponseDate { get; }
    public bool HasAcceptedResponse { get; }
    public QAndAAuthorViewModel Author { get; } = new();
    public IReadOnlyList<string> Tags { get; }

    public QAndADiscussionViewModel(QAndASearchIndexModel result)
    {
        Title = result.Title;
        DateCreated = result.PublishedDate;
        ResponseCount = result.ResponseCount;
        LatestResponseDate = result.LatestResponseDate;
        HasAcceptedResponse = Enums.TryParse<DiscussionStates>(result.DiscussionState, true, out var state)
            && state == DiscussionStates.HasAcceptedAnswer;
        Author = new(result.AuthorMemberID, result.AuthorFullName, result.AuthorUsername, result.AuthorAttributes);
        LinkPath = result.Url;
        ID = result.ID;
        Tags = result.DXTopics;
    }
}

