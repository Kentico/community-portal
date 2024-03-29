using CMS.Core;
using Kentico.Xperience.Lucene.Indexing;
using Kentico.Xperience.Lucene.Search;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Facet;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace Kentico.Community.Portal.Web.Features.QAndA.Search;

public class QAndASearchRequest
{
    public const int PAGE_SIZE = 20;

    public QAndASearchRequest(HttpRequest request)
    {
        var query = request.Query;

        DiscussionType = query.TryGetValue("discussionType", out var facetValues)
            ? facetValues.ToString()
            : "";
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

        OnlyAcceptedResponses = query.TryGetValue(nameof(OnlyAcceptedResponses), out var acceptedResponsesValues)
            && acceptedResponsesValues.Count > 0 && bool.TryParse(acceptedResponsesValues[0], out bool ar)
            && ar;
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
    public bool OnlyAcceptedResponses { get; set; }
    public string DiscussionType { get; set; } = "";

    public bool AreFiltersDefault => string.IsNullOrWhiteSpace(SearchText) && AuthorMemberID < 1 && !OnlyAcceptedResponses;
}


public class QAndASearchResultViewModel<T> : LuceneSearchResultModel<T>
{
    public string SortBy { get; set; } = "";
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

    public QAndASearchResultViewModel<QAndASearchIndexModel> SearchQAndA(QAndASearchRequest request)
    {
        var index = indexManager.GetRequiredIndex(QAndASearchIndexModel.IndexName);

        var query = GetQAndATermQuery(request);

        var combinedQuery = new BooleanQuery
        {
            { query, Occur.MUST }
        };

        if (request.DiscussionType is string facet)
        {
            var drillDownQuery = new DrillDownQuery(qAndASearchStrategy.FacetsConfigFactory());

            string[] subFacets = facet.Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (string subFacet in subFacets)
            {
                drillDownQuery.Add(nameof(QAndASearchIndexModel.DiscussionType), subFacet);
            }

            combinedQuery.Add(drillDownQuery, Occur.MUST);
        }

        try
        {
            return luceneSearchService.UseSearcherWithFacets(
                index,
                query, 20,
                (searcher, facets) =>
                {
                    var sortOptions = GetSortOption(request.SortBy);
                    var chosenSubFacets = new List<string>();
                    int pageSize = Math.Max(1, request.PageSize);
                    int pageNumber = Math.Max(1, request.PageNumber);
                    int offset = pageSize * (pageNumber - 1);
                    int limit = pageSize;

                    TopDocs topDocs = sortOptions is null
                        ? topDocs = searcher.Search(combinedQuery, MAX_RESULTS)
                        : topDocs = searcher.Search(combinedQuery, MAX_RESULTS, new Sort(sortOptions));

                    return new QAndASearchResultViewModel<QAndASearchIndexModel>
                    {
                        Query = request.SearchText ?? "",
                        Page = pageNumber,
                        PageSize = pageSize,
                        TotalPages = topDocs.TotalHits <= 0 ? 0 : ((topDocs.TotalHits - 1) / pageSize) + 1,
                        TotalHits = topDocs.TotalHits,
                        Hits = topDocs.ScoreDocs
                            .Skip(offset)
                            .Take(limit)
                            .Select(d => QAndASearchIndexModel.FromDocument(searcher.Doc(d.Doc)))
                            .ToList(),
                        Facet = request.DiscussionType,
                        Facets = facets?.GetTopChildren(10, nameof(QAndASearchIndexModel.DiscussionType), [.. chosenSubFacets])?.LabelValues.ToArray(),
                        SortBy = request.SortBy
                    };
                }
            );
        }
        catch (Exception ex)
        {
            log.LogException(nameof(QAndASearchService), "Q&A_SEARCH_FAILURE", ex);

            return new QAndASearchResultViewModel<QAndASearchIndexModel>
            {
                Facet = null,
                Facets = [],
                Hits = [],
                Page = request.PageNumber,
                PageSize = request.PageSize,
                Query = request.SearchText,
                SortBy = request.SortBy,
                TotalHits = 0,
                TotalPages = 0
            };
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
            var titleQuery = queryBuilder.CreatePhraseQuery(nameof(QAndASearchIndexModel.Title), searchText, PHRASE_SLOP);
            booleanQuery = AddToTermQuery(booleanQuery, titleQuery, 5);

            var contentQuery = queryBuilder.CreatePhraseQuery(nameof(QAndASearchIndexModel.Content), searchText, PHRASE_SLOP);
            booleanQuery = AddToTermQuery(booleanQuery, contentQuery, 1);

            var titleShould = queryBuilder.CreateBooleanQuery(nameof(QAndASearchIndexModel.Title), searchText, Occur.SHOULD);
            booleanQuery = AddToTermQuery(booleanQuery, titleShould, 0.5f);

            var contentShould = queryBuilder.CreateBooleanQuery(nameof(QAndASearchIndexModel.Content), searchText, Occur.SHOULD);
            booleanQuery = AddToTermQuery(booleanQuery, contentShould, 0.1f);
        }

        if (request.OnlyAcceptedResponses)
        {
            var bytes = new BytesRef(NumericUtils.BUF_SIZE_INT32);
            NumericUtils.Int32ToPrefixCoded(int.Parse("1"), 0, bytes);
            booleanQuery.Add(new TermQuery(new Term(nameof(QAndASearchIndexModel.HasAcceptedResponse), bytes)), Occur.MUST);
        }

        return booleanQuery;
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
