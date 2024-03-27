using CMS.Core;
using Kentico.Community.Portal.Web.Features.QAndA;
using Kentico.Xperience.Lucene.Indexing;
using Kentico.Xperience.Lucene.Search;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Facet;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace Kentico.Community.Portal.Web.Features.Blog.Components;

public class BlogSearchResultViewModel<T> : LuceneSearchResultModel<T>
{
    public string SortBy { get; set; } = "";
}

public class BlogSearchService(
    ILuceneSearchService luceneSearchService,
    IEventLogService log,
    BlogSearchIndexingStrategy blogSearchStrategy)
{
    private const int PHRASE_SLOP = 3;
    private const int MAX_RESULTS = 1000;

    private readonly ILuceneSearchService luceneSearchService = luceneSearchService;
    private readonly IEventLogService log = log;
    private readonly BlogSearchIndexingStrategy blogSearchStrategy = blogSearchStrategy;

    public LuceneSearchResultModel<BlogSearchModel> SearchBlog(BlogSearchRequest request)
    {
        var index = LuceneIndexStore.Instance.GetRequiredIndex(BlogSearchModel.IndexName);

        var query = GetBlogTermQuery(request);

        var combinedQuery = new BooleanQuery
        {
            { query, Occur.MUST }
        };

        if (request.Facet is string facet)
        {
            var drillDownQuery = new DrillDownQuery(blogSearchStrategy.FacetsConfigFactory());

            string[] subFacets = facet.Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (string subFacet in subFacets)
            {
                drillDownQuery.Add(nameof(BlogSearchModel.Taxonomy), subFacet);
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

                    TopDocs topDocs = sortOptions is null
                        ? topDocs = searcher.Search(combinedQuery, MAX_RESULTS)
                        : topDocs = searcher.Search(combinedQuery, MAX_RESULTS, new Sort(sortOptions));

                    var chosenSubFacets = new List<string>();
                    int pageSize = Math.Max(1, request.PageSize);
                    int pageNumber = Math.Max(1, request.PageNumber);
                    int offset = pageSize * (pageNumber - 1);
                    int limit = pageSize;

                    return new BlogSearchResultViewModel<BlogSearchModel>
                    {
                        Query = request.SearchText ?? "",
                        Page = pageNumber,
                        PageSize = pageSize,
                        TotalPages = topDocs.TotalHits <= 0 ? 0 : ((topDocs.TotalHits - 1) / pageSize) + 1,
                        TotalHits = topDocs.TotalHits,
                        Hits = topDocs.ScoreDocs
                            .Skip(offset)
                            .Take(limit)
                            .Select(d => BlogSearchModel.FromDocument(searcher.Doc(d.Doc)))
                            .ToList(),
                        Facet = request.Facet,
                        Facets = facets?.GetTopChildren(10, nameof(BlogSearchModel.Taxonomy), [.. chosenSubFacets])?.LabelValues.ToArray(),
                        SortBy = request.SortBy
                    };
                }
            );
        }
        catch (Exception ex)
        {
            log.LogException(nameof(BlogSearchService), "BLOG_SEARCH_FAILURE", ex);

            return new BlogSearchResultViewModel<BlogSearchModel>
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
            var authorQuery = NumericRangeQuery.NewInt32Range(nameof(BlogSearchModel.AuthorMemberID), request.AuthorMemberID, request.AuthorMemberID, true, true);
            booleanQuery.Add(authorQuery, Occur.MUST);
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
            var queryBuilder = new QueryBuilder(analyzer);
            var titleQuery = queryBuilder.CreatePhraseQuery(nameof(BlogSearchModel.Title), searchText, PHRASE_SLOP);
            booleanQuery = AddToTermQuery(booleanQuery, titleQuery, 5);

            var contentQuery = queryBuilder.CreatePhraseQuery(nameof(BlogSearchModel.Content), searchText, PHRASE_SLOP);
            booleanQuery = AddToTermQuery(booleanQuery, contentQuery, 1);

            var titleShould = queryBuilder.CreateBooleanQuery(nameof(BlogSearchModel.Title), searchText, Occur.SHOULD);
            booleanQuery = AddToTermQuery(booleanQuery, titleShould, 0.5f);

            var contentShould = queryBuilder.CreateBooleanQuery(nameof(BlogSearchModel.Content), searchText, Occur.SHOULD);
            booleanQuery = AddToTermQuery(booleanQuery, contentShould, 0.1f);
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
            "date" or "publishdate" => new SortField(nameof(QAndASearchModel.PublishedDate), FieldCache.NUMERIC_UTILS_INT64_PARSER, true),
            _ => null,
        };
}
