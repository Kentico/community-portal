using Kentico.Community.Portal.Web.Features.Blog.Models;
using Kentico.Community.Portal.Web.Features.QAndA;
using Kentico.Xperience.Lucene;
using Kentico.Xperience.Lucene.Models;
using Kentico.Xperience.Lucene.Services;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Facet;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace Kentico.Community.Portal.Web.Infrastructure.Search;

public class GlobalSearchResultViewModel<T> : LuceneSearchResultModel<T>
{
    public string SortBy { get; set; } = "";
}

public class SearchService
{
    private const int PHRASE_SLOP = 3;
    private const int MAX_RESULTS = 1000;

    private readonly ILuceneIndexService luceneIndexService;

    public SearchService(ILuceneIndexService luceneIndexService) => this.luceneIndexService = luceneIndexService;

    public LuceneSearchResultModel<BlogSearchResult> SearchBlog(BlogSearchRequest request)
    {
        var (searchText, facet, sortBy, pageNumber, pageSize) = request;

        var index = IndexStore.Instance.GetIndex(BlogSearchModel.IndexName) ?? throw new Exception($"Index {BlogSearchModel.IndexName} was not found!!!");

        var query = GetBlogTermQuery(searchText);

        var combinedQuery = new BooleanQuery
        {
            { query, Occur.MUST }
        };

        if (facet != null)
        {
            var indexingStrategy = new BlogSearchIndexingStrategy();
            var drillDownQuery = new DrillDownQuery(indexingStrategy.FacetsConfigFactory());

            string[] subFacets = facet.Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (string subFacet in subFacets)
            {
                drillDownQuery.Add(nameof(BlogSearchModel.Taxonomy), subFacet);
            }

            combinedQuery.Add(drillDownQuery, Occur.MUST);
        }

        var result = luceneIndexService.UseSearcherWithFacets(
            index,
            query, 20,
            (searcher, facets) =>
            {
                var sortOptions = GetSortOption(sortBy);

                TopDocs topDocs = sortOptions == null
                    ? topDocs = searcher.Search(combinedQuery, MAX_RESULTS)
                    : topDocs = searcher.Search(combinedQuery, MAX_RESULTS, new Sort(sortOptions));

                var chosenSubFacets = new List<string>();
                pageSize = Math.Max(1, pageSize);
                pageNumber = Math.Max(1, pageNumber);
                int offset = pageSize * (pageNumber - 1);
                int limit = pageSize;

                return new GlobalSearchResultViewModel<BlogSearchResult>
                {
                    Query = searchText ?? "",
                    Page = pageNumber,
                    PageSize = pageSize,
                    TotalPages = topDocs.TotalHits <= 0 ? 0 : ((topDocs.TotalHits - 1) / pageSize) + 1,
                    TotalHits = topDocs.TotalHits,
                    Hits = topDocs.ScoreDocs
                        .Skip(offset)
                        .Take(limit)
                        .Select(d => BlogSearchResult.MapFromDocument(searcher.Doc(d.Doc)))
                        .ToList(),
                    Facet = facet,
                    Facets = facets?.GetTopChildren(10, nameof(BlogSearchModel.Taxonomy), chosenSubFacets.ToArray())?.LabelValues.ToArray(),
                    SortBy = sortBy
                };
            }
        );

        return result;
    }

    private static Query GetBlogTermQuery(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return new MatchAllDocsQuery();
        }

        var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
        var queryBuilder = new QueryBuilder(analyzer);
        var booleanQuery = new BooleanQuery();
        var titleQuery = queryBuilder.CreatePhraseQuery(nameof(BlogSearchModel.Title), searchText, PHRASE_SLOP);
        booleanQuery = AddToTermQuery(booleanQuery, titleQuery, 5);

        var contentQuery = queryBuilder.CreatePhraseQuery(nameof(BlogSearchModel.Content), searchText, PHRASE_SLOP);
        booleanQuery = AddToTermQuery(booleanQuery, contentQuery, 1);

        var titleShould = queryBuilder.CreateBooleanQuery(nameof(BlogSearchModel.Title), searchText, Occur.SHOULD);
        booleanQuery = AddToTermQuery(booleanQuery, titleShould, 0.5f);

        var contentShould = queryBuilder.CreateBooleanQuery(nameof(BlogSearchModel.Content), searchText, Occur.SHOULD);
        booleanQuery = AddToTermQuery(booleanQuery, contentShould, 0.1f);
        return booleanQuery;
    }

    public GlobalSearchResultViewModel<QAndASearchResult> SearchQAndA(QAndASearchRequest request)
    {
        var index = IndexStore.Instance.GetIndex(QAndASearchModel.IndexName) ?? throw new Exception($"Index {QAndASearchModel.IndexName} was not found!!!");

        var (searchText, sortBy, pageNumber, pageSize) = request;

        var query = GetQAndATermQuery(searchText);

        var result = luceneIndexService.UseSearcher(
            index,
            (searcher) =>
            {
                var sortOptions = GetSortOption(sortBy);

                var topDocs = sortOptions == null
                    ? searcher.Search(query, MAX_RESULTS)
                    : searcher.Search(query, MAX_RESULTS, new Sort(sortOptions));

                pageSize = Math.Max(1, pageSize);
                pageNumber = Math.Max(1, pageNumber);

                int offset = pageSize * (pageNumber - 1);
                int limit = pageSize;

                return new GlobalSearchResultViewModel<QAndASearchResult>
                {
                    Query = searchText ?? "",
                    Page = pageNumber,
                    PageSize = pageSize,
                    TotalPages = topDocs.TotalHits <= 0 ? 0 : ((topDocs.TotalHits - 1) / pageSize) + 1,
                    TotalHits = topDocs.TotalHits,
                    Hits = topDocs.ScoreDocs
                        .Skip(offset)
                        .Take(limit)
                        .Select(d => QAndASearchResult.MapFromDocument(searcher.Doc(d.Doc)))
                        .ToList(),
                    SortBy = sortBy
                };
            }
        );

        return result ?? new GlobalSearchResultViewModel<QAndASearchResult>();
    }

    private static Query GetQAndATermQuery(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return new MatchAllDocsQuery();
        }

        var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
        var queryBuilder = new QueryBuilder(analyzer);
        var booleanQuery = new BooleanQuery();
        var titleQuery = queryBuilder.CreatePhraseQuery(nameof(QAndASearchModel.Title), searchText, PHRASE_SLOP);
        booleanQuery = AddToTermQuery(booleanQuery, titleQuery, 5);

        var contentQuery = queryBuilder.CreatePhraseQuery(nameof(QAndASearchModel.Content), searchText, PHRASE_SLOP);
        booleanQuery = AddToTermQuery(booleanQuery, contentQuery, 1);

        var titleShould = queryBuilder.CreateBooleanQuery(nameof(QAndASearchModel.Title), searchText, Occur.SHOULD);
        booleanQuery = AddToTermQuery(booleanQuery, titleShould, 0.5f);

        var contentShould = queryBuilder.CreateBooleanQuery(nameof(QAndASearchModel.Content), searchText, Occur.SHOULD);
        booleanQuery = AddToTermQuery(booleanQuery, contentShould, 0.1f);

        return booleanQuery;
    }

    private static BooleanQuery AddToTermQuery(BooleanQuery query, Query textQueryPart, float boost)
    {
        textQueryPart.Boost = boost;
        query.Add(textQueryPart, Occur.SHOULD);

        return query;
    }

    private static SortField GetSortOption(string sortBy = null) =>
        sortBy switch
        {
            "date" => new SortField(nameof(QAndASearchModel.DateMilliseconds), FieldCache.NUMERIC_UTILS_INT64_PARSER, true),
            _ => null,
        };
}
