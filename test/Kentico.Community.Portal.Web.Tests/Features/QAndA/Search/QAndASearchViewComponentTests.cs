using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Components.ViewComponents.Pagination;
using Kentico.Community.Portal.Web.Features.QAndA;
using Kentico.Community.Portal.Web.Features.QAndA.Search;
using Lucene.Net.Facet;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Kentico.Community.Portal.Web.Tests.Features.QAndA.Search;

public class QAndASearchViewComponentTests
{
    [Test]
    public void GetRouteData_Includes_DiscussionStates_In_Pagination_Links()
    {
        // Arrange
        var request = new QAndASearchRequestTestBuilder()
            .WithDiscussionStates(["HasAcceptedAnswer", "NoAcceptedAnswer"])
            .WithPageNumber(1)
            .Build();

        var result = new QAndASearchResultTestBuilder()
            .WithTotalPages(3)
            .Build();

        var discussions = new List<QAndADiscussionViewModel>();
        var taxonomies = new QAndATaxonomiesQueryResponseTestBuilder().Build();

        var sut = new QAndASearchViewModel(request, result, discussions, taxonomies);

        // Act
        IPagedViewModel pagedViewModel = sut;
        string pageLinkPath = pagedViewModel.PageLinkPath(2, "/q-and-a");

        // Assert
        Assert.That(pageLinkPath, Does.Contain("discussionStates=hasacceptedanswer"));
        Assert.That(pageLinkPath, Does.Contain("discussionStates=noacceptedanswer"));
        Assert.That(pageLinkPath, Does.Contain("page=2"));
    }

    [Test]
    public void GetRouteData_Excludes_DiscussionStates_When_None_Selected()
    {
        // Arrange
        var request = new QAndASearchRequestTestBuilder()
            .WithPageNumber(1)
            .Build();

        var result = new QAndASearchResultTestBuilder()
            .WithTotalPages(3)
            .Build();

        var discussions = new List<QAndADiscussionViewModel>();
        var taxonomies = new QAndATaxonomiesQueryResponseTestBuilder().Build();

        var sut = new QAndASearchViewModel(request, result, discussions, taxonomies);

        // Act
        IPagedViewModel pagedViewModel = sut;
        string pageLinkPath = pagedViewModel.PageLinkPath(2, "/q-and-a");

        // Assert
        Assert.That(pageLinkPath, Does.Not.Contain("discussionStates"));
        Assert.That(pageLinkPath, Does.Contain("page=2"));
    }

    [Test]
    public void GetRouteData_Includes_Multiple_DiscussionStates_Parameters()
    {
        // Arrange
        var request = new QAndASearchRequestTestBuilder()
            .WithDiscussionStates(["HasAcceptedAnswer", "NoAcceptedAnswer"])
            .WithSearchText("test query")
            .WithPageNumber(1)
            .Build();

        var result = new QAndASearchResultTestBuilder()
            .WithTotalPages(5)
            .Build();

        var discussions = new List<QAndADiscussionViewModel>();
        var taxonomies = new QAndATaxonomiesQueryResponseTestBuilder().Build();

        var sut = new QAndASearchViewModel(request, result, discussions, taxonomies);

        // Act
        IPagedViewModel pagedViewModel = sut;
        string pageLinkPath = pagedViewModel.PageLinkPath(3, "/q-and-a");

        // Assert - Verify multiple discussionStates parameters are included
        Assert.That(pageLinkPath, Does.Contain("discussionStates=hasacceptedanswer"));
        Assert.That(pageLinkPath, Does.Contain("discussionStates=noacceptedanswer"));
        Assert.That(pageLinkPath, Does.Contain("page=3"));
        Assert.That(pageLinkPath, Does.Contain("query=test%20query"));
    }
}

public class QAndASearchRequestTestBuilder
{
    private string searchText = "";
    private string sortBy = "activitydate";
    private int pageNumber = 1;
    private IEnumerable<string> discussionStates = [];
    private IEnumerable<string> discussionTypes = [];
    private IEnumerable<string> dxTopics = [];

    public QAndASearchRequestTestBuilder WithSearchText(string text)
    {
        searchText = text;
        return this;
    }

    public QAndASearchRequestTestBuilder WithSortBy(string sort)
    {
        sortBy = sort;
        return this;
    }

    public QAndASearchRequestTestBuilder WithPageNumber(int page)
    {
        pageNumber = page;
        return this;
    }

    public QAndASearchRequestTestBuilder WithDiscussionStates(IEnumerable<string> states)
    {
        discussionStates = states;
        return this;
    }

    public QAndASearchRequestTestBuilder WithDiscussionTypes(IEnumerable<string> types)
    {
        discussionTypes = types;
        return this;
    }

    public QAndASearchRequestTestBuilder WithDXTopics(IEnumerable<string> topics)
    {
        dxTopics = topics;
        return this;
    }

    public QAndASearchRequest Build()
    {
        // Create a mock HttpRequest
        var httpContext = Substitute.For<HttpContext>();
        var httpRequest = Substitute.For<HttpRequest>();
        var queryCollection = Substitute.For<IQueryCollection>();

        // Setup query parameters
        var queryDict = new Dictionary<string, StringValues>();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            queryDict["query"] = new StringValues(searchText);
        }
        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            queryDict["sortBy"] = new StringValues(sortBy);
        }
        if (pageNumber > 1)
        {
            queryDict["page"] = new StringValues(pageNumber.ToString());
        }
        if (discussionStates.Any())
        {
            queryDict["discussionStates"] = new StringValues(discussionStates.ToArray());
        }
        if (discussionTypes.Any())
        {
            queryDict["discussionTypes"] = new StringValues(discussionTypes.ToArray());
        }
        if (dxTopics.Any())
        {
            queryDict["dxTopics"] = new StringValues(dxTopics.ToArray());
        }

        // Setup the query collection to return our dictionary values
        queryCollection.GetEnumerator().Returns(queryDict.GetEnumerator());
        foreach (var kvp in queryDict)
        {
            queryCollection.TryGetValue(kvp.Key, out Arg.Any<StringValues>())
                .Returns(x =>
                {
                    x[1] = kvp.Value;
                    return true;
                });
        }

        httpRequest.Query.Returns(queryCollection);
        httpContext.Request.Returns(httpRequest);

        return new QAndASearchRequest(httpRequest);
    }
}

public class QAndASearchResultTestBuilder
{
    private int totalPages = 1;
    private int totalHits = 0;
    private List<QAndASearchIndexModel> hits = [];
    private LabelAndValue[] discussionTypes = [];
    private LabelAndValue[] discussionStates = [];
    private LabelAndValue[] dxTopics = [];

    public QAndASearchResultTestBuilder WithTotalPages(int pages)
    {
        totalPages = pages;
        return this;
    }

    public QAndASearchResultTestBuilder WithTotalHits(int total)
    {
        totalHits = total;
        return this;
    }

    public QAndASearchResultTestBuilder WithHits(List<QAndASearchIndexModel> searchHits)
    {
        hits = searchHits;
        return this;
    }

    public QAndASearchResultTestBuilder WithDiscussionTypes(LabelAndValue[] types)
    {
        discussionTypes = types;
        return this;
    }

    public QAndASearchResultTestBuilder WithDiscussionStates(LabelAndValue[] states)
    {
        discussionStates = states;
        return this;
    }

    public QAndASearchResultTestBuilder WithDXTopics(LabelAndValue[] topics)
    {
        dxTopics = topics;
        return this;
    }

    public QAndASearchResult Build()
    {
        var result = QAndASearchResult.Empty(new QAndASearchRequest("activitydate", QAndASearchRequest.PAGE_SIZE));
        result.TotalPages = totalPages;
        result.TotalHits = totalHits;
        result.Hits = hits;
        result.DiscussionTypes = discussionTypes;
        result.DiscussionStates = discussionStates;
        result.DXTopics = dxTopics;
        return result;
    }
}

public class QAndATaxonomiesQueryResponseTestBuilder
{
    private List<TaxonomyTag> types = [];
    private List<TaxonomyTag> dxTopicsHierarchy = [];
    private List<TaxonomyTag> dxTopicsAll = [];

    public QAndATaxonomiesQueryResponseTestBuilder WithTypes(List<TaxonomyTag> taxonomyTypes)
    {
        types = taxonomyTypes;
        return this;
    }

    public QAndATaxonomiesQueryResponseTestBuilder WithDXTopicsHierarchy(List<TaxonomyTag> hierarchy)
    {
        dxTopicsHierarchy = hierarchy;
        return this;
    }

    public QAndATaxonomiesQueryResponseTestBuilder WithDXTopicsAll(List<TaxonomyTag> allTopics)
    {
        dxTopicsAll = allTopics;
        return this;
    }

    public QAndATaxonomiesQueryResponse Build() =>
        new(types, dxTopicsHierarchy, dxTopicsAll);
}
