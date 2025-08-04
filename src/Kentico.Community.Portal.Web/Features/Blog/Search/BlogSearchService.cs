using CMS.Core;
using Kentico.Xperience.Lucene.Core.Indexing;
using Kentico.Xperience.Lucene.Core.Search;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Facet;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace Kentico.Community.Portal.Web.Features.Blog.Search;

public class BlogSearchRequest
{
    public const int PAGE_SIZE = 10;

    public BlogSearchRequest(HttpRequest request)
    {
        var query = request.Query;

        BlogTypes = query.TryGetValue("blogTypes", out var blogTypeValues)
            ? blogTypeValues.WhereNotNullOrWhiteSpace().ToList()
            : [];
        DXTopics = query.TryGetValue("dxTopics", out var topicValues)
            ? topicValues.WhereNotNullOrWhiteSpace().ToList()
            : [];
        SearchText = query.TryGetValue("query", out var queryValues)
            ? queryValues.ToString()
            : "";
        SortBy = query.TryGetValue("sortBy", out var sortByValues)
            ? sortByValues.TryFirst().TryGetValue(out string? sortBy)
                ? sortBy
                : "publishdate"
            : "publishdate";
        PageNumber = query.TryGetValue("page", out var pageValues)
            ? int.TryParse(pageValues, out int p)
                ? p
                : 1
            : 1;
    }

    public BlogSearchRequest(string sortBy, int pageSize)
    {
        SortBy = sortBy;
        PageSize = pageSize;
    }

    public IEnumerable<string> BlogTypes { get; } = [];
    public IEnumerable<string> DXTopics { get; } = [];
    public string SearchText { get; } = "";
    public string SortBy { get; } = "";
    public int PageNumber { get; } = 1;
    public int AuthorMemberID { get; set; }
    public int PageSize { get; } = PAGE_SIZE;

    public bool AreFiltersDefault => string.IsNullOrWhiteSpace(SearchText) && AuthorMemberID < 1;
}

public class BlogSearchResults
{
    public string Query { get; init; } = "";
    public IEnumerable<BlogSearchIndexModel> Hits { get; } = [];
    public int TotalHits { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; init; }
    public int Page { get; init; }
    public LabelAndValue[] BlogTypes { get; } = [];
    public LabelAndValue[] DXTopics { get; } = [];
    public string SortBy { get; init; } = "";

    public static BlogSearchResults Empty(BlogSearchRequest request) => new()
    {
        Page = request.PageNumber,
        PageSize = request.PageSize,
        Query = request.SearchText,
        SortBy = request.SortBy,
    };

    public BlogSearchResults(TopDocs topDocs, MultiFacets facets, BlogSearchRequest request, Func<ScoreDoc, Document> retrieveDoc)
    {
        /*
        * This is performing "fake" paging. We request all the results and then
        * offset/limit from there.
        * This is normal for Lucene.NET because the ScoreDocs are very lightweight
        * until they are actually retrieved from the index using searcher.Doc()
        * https://stackoverflow.com/a/8287427/939634
        */
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
            .Select(d => BlogSearchIndexModel.FromDocument(retrieveDoc(d)))
            .ToList();
        BlogTypes = facets.GetTopChildren(100, nameof(BlogSearchIndexModel.BlogTypeFacet))?.LabelValues.ToArray() ?? [];
        DXTopics = facets.GetTopChildren(100, nameof(BlogSearchIndexModel.DXTopicsFacets))?.LabelValues.ToArray() ?? [];
        SortBy = request.SortBy;
    }

    private BlogSearchResults() { }
}

public class BlogSearchService(
    ILuceneSearchService luceneSearchService,
    IEventLogService log,
    BlogSearchIndexingStrategy blogSearchStrategy,
    ILuceneIndexManager indexManager)
{
    private const int PHRASE_SLOP = 3;
    private const int MAX_RESULTS = 1000;

    private readonly ILuceneSearchService luceneSearchService = luceneSearchService;
    private readonly IEventLogService log = log;
    private readonly BlogSearchIndexingStrategy blogSearchStrategy = blogSearchStrategy;
    private readonly ILuceneIndexManager indexManager = indexManager;

    public BlogSearchResults SearchBlog(BlogSearchRequest request)
    {
        var index = indexManager.GetRequiredIndex(BlogSearchIndexModel.IndexName);

        var query = GetBlogTermQuery(request);

        var combinedQuery = AddFacetsToQuery(
            request,
            new BooleanQuery
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

                    return new BlogSearchResults(topDocs, facets, request, d => searcher.Doc(d.Doc));
                }
            );
        }
        catch (Exception ex)
        {
            log.LogException(nameof(BlogSearchService), "BLOG_SEARCH_FAILURE", ex);

            return BlogSearchResults.Empty(request);
        }
    }

    private Query AddFacetsToQuery(BlogSearchRequest request, Query baseQuery)
    {
        if (!request.BlogTypes.Any() && !request.DXTopics.Any())
        {
            return baseQuery;
        }

        var drillDownQuery = new DrillDownQuery(blogSearchStrategy.FacetsConfigFactory(), baseQuery);

        foreach (string blogType in request.BlogTypes)
        {
            drillDownQuery.Add(nameof(BlogSearchIndexModel.BlogTypeFacet), blogType.ToLowerInvariant());
        }

        foreach (string topic in request.DXTopics)
        {
            drillDownQuery.Add(nameof(BlogSearchIndexModel.DXTopicsFacets), topic.ToLowerInvariant());
        }

        return drillDownQuery;
    }

    private static Query GetBlogTermQuery(BlogSearchRequest request)
    {
        string searchText = request.SearchText.Trim();

        if (request.AreFiltersDefault)
        {
            return new MatchAllDocsQuery();
        }

        var booleanQuery = new BooleanQuery();

        if (request.AuthorMemberID > 0)
        {
            var authorQuery = NumericRangeQuery.NewInt32Range(nameof(BlogSearchIndexModel.AuthorMemberID), request.AuthorMemberID, request.AuthorMemberID, true, true);
            booleanQuery.Add(authorQuery, Occur.MUST);
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var (slop, term) = searchText switch
            {
            ['"', .., '"'] => (0, searchText.Trim('"')),
                _ => (PHRASE_SLOP, searchText)
            };

            var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
            var queryBuilder = new QueryBuilder(analyzer);
            var titleQuery = queryBuilder.CreatePhraseQuery(nameof(BlogSearchIndexModel.Title), term, slop);
            booleanQuery = AddToTermQuery(booleanQuery, titleQuery, 5);

            var contentQuery = queryBuilder.CreatePhraseQuery(nameof(BlogSearchIndexModel.Content), term, slop);
            booleanQuery = AddToTermQuery(booleanQuery, contentQuery, 1);

            if (slop > 0)
            {
                var titleShould = queryBuilder.CreateBooleanQuery(nameof(BlogSearchIndexModel.Title), term, Occur.SHOULD);
                booleanQuery = AddToTermQuery(booleanQuery, titleShould, 0.5f);

                var contentShould = queryBuilder.CreateBooleanQuery(nameof(BlogSearchIndexModel.Content), term, Occur.SHOULD);
                booleanQuery = AddToTermQuery(booleanQuery, contentShould, 0.1f);
            }
        }

        return booleanQuery;
    }

    private static BooleanQuery AddToTermQuery(BooleanQuery query, Query textQueryPart, float boost)
    {
        if (textQueryPart is null)
        {
            return query;
        }

        textQueryPart.Boost = boost;
        query.Add(textQueryPart, Occur.SHOULD);

        return query;
    }

    private static SortField? GetSortOption(string? sortBy = null) =>
        sortBy switch
        {
            "publishdate" => new SortField(nameof(BlogSearchIndexModel.PublishedDate), FieldCache.NUMERIC_UTILS_INT64_PARSER, true),
            _ => null,
        };
}
