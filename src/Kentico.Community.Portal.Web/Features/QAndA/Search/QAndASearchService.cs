using System.ComponentModel;
using CMS.Core;
using Kentico.Xperience.Lucene.Core.Indexing;
using Kentico.Xperience.Lucene.Core.Search;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Facet;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace Kentico.Community.Portal.Web.Features.QAndA.Search;

public class QAndASearchRequest
{
    public const int PAGE_SIZE = 20;

    public QAndASearchRequest(HttpRequest request)
    {
        var query = request.Query;

        DiscussionTypes = query.TryGetValue("discussionTypes", out var facetValues)
            ? facetValues
            : [];
        DXTopics = query.TryGetValue("dxTopics", out var topicValues)
            ? topicValues
            : [];
        DiscussionStates = query.TryGetValue("discussionStates", out var discussionStates)
            ? discussionStates
            : [];
        SearchText = query.TryGetValue("query", out var queryValues)
            ? queryValues.ToString()
            : "";
        SortBy = query.TryGetValue("sortBy", out var sortByValues)
            ? sortByValues.ToString()
            : "publishdate";
        PageNumber = query.TryGetValue("page", out var pageValues)
            ? int.TryParse(pageValues, out int p)
                ? p
                : 1
            : 1;

        AuthorMemberID = query.TryGetValue("author", out var authorValues)
            ? int.TryParse(authorValues, out int a)
                ? a
                : 0
            : 0;
    }

    public QAndASearchRequest(string sortBy, int pageSize)
    {
        SortBy = sortBy;
        PageSize = pageSize;
    }

    public string SearchText { get; } = "";
    public string SortBy { get; } = "";
    public int PageNumber { get; } = 1;
    public int PageSize { get; } = PAGE_SIZE;
    public int AuthorMemberID { get; set; }
    public IEnumerable<string> DiscussionStates { get; } = [];
    public IEnumerable<string> DiscussionTypes { get; set; } = [];
    public IEnumerable<string> DXTopics { get; set; } = [];

    public bool AreFiltersDefault =>
        string.IsNullOrWhiteSpace(SearchText)
        && AuthorMemberID < 1;
}

public enum DiscussionStates
{
    [Description("Has accepted answer")]
    HasAcceptedAnswer,
    [Description("No accepted answer")]
    NoAcceptedAnswer
}


public class QAndASearchResult
{
    public string Query { get; set; } = "";
    public IEnumerable<QAndASearchIndexModel> Hits { get; set; } = [];
    public int TotalHits { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public int Page { get; set; }
    public LabelAndValue[] DiscussionTypes { get; set; } = [];
    public LabelAndValue[] DXTopics { get; set; } = [];
    public LabelAndValue[] DiscussionStates { get; set; } = [];
    public string SortBy { get; set; } = "";

    public static QAndASearchResult Empty(QAndASearchRequest request) =>
        new()
        {
            Page = request.PageNumber,
            PageSize = request.PageSize,
            Query = request.SearchText,
            SortBy = request.SortBy,
        };

    public QAndASearchResult(TopDocs topDocs, MultiFacets facets, QAndASearchRequest request, Func<ScoreDoc, Document> retrieveDoc)
    {
        int pageSize = Math.Max(1, request.PageSize);
        int pageNumber = Math.Max(1, request.PageNumber);
        int offset = pageSize * (pageNumber - 1);
        int limit = pageSize;

        Query = request.SearchText ?? "";
        Page = pageNumber;
        PageSize = pageSize;
        TotalPages = topDocs.TotalHits <= 0 ? 0 : ((topDocs.TotalHits - 1) / pageSize) + 1;
        TotalHits = topDocs.TotalHits;
        Hits = topDocs.ScoreDocs
            .Skip(offset)
            .Take(limit)
            .Select(d => QAndASearchIndexModel.FromDocument(retrieveDoc(d)))
            .ToList();
        DiscussionTypes = facets.GetTopChildren(2, nameof(QAndASearchIndexModel.DiscussionTypeFacet), [])?.LabelValues.ToArray() ?? [];
        DiscussionStates = facets.GetTopChildren(2, nameof(QAndASearchIndexModel.DiscussionStatesFacet), [])?.LabelValues.ToArray() ?? [];
        DXTopics = facets.GetTopChildren(100, nameof(QAndASearchIndexModel.DXTopicsFacet), [])?.LabelValues.ToArray() ?? [];
        SortBy = request.SortBy;
    }

    private QAndASearchResult() { }
}

public class QAndASearchService(
    ILuceneSearchService luceneSearchService,
    IEventLogService log,
    QAndASearchIndexingStrategy qAndASearchStrategy,
    ILuceneIndexManager indexManager)
{
    private const int PHRASE_SLOP = 3;
    private const int MAX_RESULTS = 1000;

    private readonly ILuceneSearchService luceneSearchService = luceneSearchService;
    private readonly IEventLogService log = log;
    private readonly QAndASearchIndexingStrategy qAndASearchStrategy = qAndASearchStrategy;
    private readonly ILuceneIndexManager indexManager = indexManager;

    public QAndASearchResult SearchQAndA(QAndASearchRequest request)
    {
        var index = indexManager.GetRequiredIndex(QAndASearchIndexModel.IndexName);

        var query = GetQAndATermQuery(request);

        var combinedQuery = FacetsQuery(request, new BooleanQuery
        {
            { query, Occur.MUST }
        });

        try
        {
            return luceneSearchService.UseSearcherWithFacets(
                index,
                combinedQuery, 20,
                (searcher, facets) =>
                {
                    var sortOptions = GetSortOption(request.SortBy);

                    TopDocs topDocs = sortOptions is null
                        ? topDocs = searcher.Search(combinedQuery, MAX_RESULTS)
                        : topDocs = searcher.Search(combinedQuery, MAX_RESULTS, new Sort(sortOptions));

                    return new QAndASearchResult(topDocs, facets, request, (d) => searcher.Doc(d.Doc));
                }
            );
        }
        catch (Exception ex)
        {
            log.LogException(nameof(QAndASearchService), "Q&A_SEARCH_FAILURE", ex);

            return QAndASearchResult.Empty(request);
        }
    }

    private static Query GetQAndATermQuery(QAndASearchRequest request)
    {
        string searchText = request.SearchText.Trim();

        if (request.AreFiltersDefault)
        {
            return new MatchAllDocsQuery();
        }

        var booleanQuery = new BooleanQuery();

        if (request.AuthorMemberID > 0)
        {
            var authorQuery = NumericRangeQuery.NewInt32Range(nameof(QAndASearchIndexModel.AuthorMemberID), request.AuthorMemberID, request.AuthorMemberID, true, true);
            booleanQuery.Add(authorQuery, Occur.MUST);
        }

        var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
        var queryBuilder = new QueryBuilder(analyzer);
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var (slop, term) = searchText switch
            {
                ['"', .., '"'] => (0, searchText.Trim('"')),
                _ => (PHRASE_SLOP, searchText)
            };

            var titleQuery = queryBuilder.CreatePhraseQuery(nameof(QAndASearchIndexModel.Title), term, slop);
            booleanQuery = AddToTermQuery(booleanQuery, titleQuery, 5);

            var contentQuery = queryBuilder.CreatePhraseQuery(nameof(QAndASearchIndexModel.Content), term, slop);
            booleanQuery = AddToTermQuery(booleanQuery, contentQuery, 1);

            if (slop > 0)
            {
                var titleShould = queryBuilder.CreateBooleanQuery(nameof(QAndASearchIndexModel.Title), term, Occur.SHOULD);
                booleanQuery = AddToTermQuery(booleanQuery, titleShould, 0.5f);

                var contentShould = queryBuilder.CreateBooleanQuery(nameof(QAndASearchIndexModel.Content), term, Occur.SHOULD);
                booleanQuery = AddToTermQuery(booleanQuery, contentShould, 0.1f);
            }
        }

        return booleanQuery;
    }

    private Query FacetsQuery(QAndASearchRequest request, Query query)
    {
        if (!request.DiscussionTypes.Any() && !request.DXTopics.Any() && !request.DiscussionStates.Any())
        {
            return query;
        }

        var drillDownQuery = new DrillDownQuery(qAndASearchStrategy.FacetsConfigFactory(), query);

        foreach (string discussionType in request.DiscussionTypes)
        {
            drillDownQuery.Add(nameof(QAndASearchIndexModel.DiscussionTypeFacet), discussionType);
        }

        foreach (string topic in request.DXTopics)
        {
            drillDownQuery.Add(nameof(QAndASearchIndexModel.DXTopicsFacet), topic);
        }

        foreach (string state in request.DiscussionStates)
        {
            drillDownQuery.Add(nameof(QAndASearchIndexModel.DiscussionStatesFacet), state);
        }

        return drillDownQuery;
    }

    private static BooleanQuery AddToTermQuery(BooleanQuery query, Query textQueryPart, float boost)
    {
        textQueryPart.Boost = boost;
        query.Add(textQueryPart, Occur.SHOULD);

        return query;
    }

    private static SortField? GetSortOption(string? sortBy = null) =>
        sortBy switch
        {
            "publishdate" => new SortField(nameof(QAndASearchIndexModel.PublishedDate), FieldCache.NUMERIC_UTILS_INT64_PARSER, true),
            "responsedate" => new SortField(nameof(QAndASearchIndexModel.LatestResponseDate), FieldCache.NUMERIC_UTILS_INT64_PARSER, true),
            _ => null,
        };
}
